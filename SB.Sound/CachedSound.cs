using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SB.SoundPlayer
{
    /// <summary>
    /// A small sound decoded into memory once, so that starting several copies of it at the same moment
    /// does not open and decode the file again for each copy.
    /// </summary>
    public class CachedSound
    {
        private const long MaxFileBytes = 2 * 1024 * 1024;
        private const int MaxSamples = 8 * 1024 * 1024;

        private static readonly object _lock = new object();
        private static CachedSound _last;

        public string FileName { get; private set; }
        public DateTime Stamp { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public float[] Samples { get; private set; }

        /// <summary>The decoded sound, or null when the file is too big to keep in memory (it is then streamed as usual).</summary>
        public static CachedSound Get(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            if (!info.Exists || info.Length > MaxFileBytes) return null;

            lock (_lock)
            {
                if (_last != null && _last.FileName == fileName && _last.Stamp == info.LastWriteTimeUtc)
                    return _last;

                CachedSound sound = Decode(fileName, info.LastWriteTimeUtc);
                _last = sound;
                return sound;
            }
        }

        private static CachedSound Decode(string fileName, DateTime stamp)
        {
            IAudioReader input;
            try
            {
                input = new FileReader(fileName);
            }
            catch (COMException)
            {
                input = new OggFileReader(fileName);
            }

            try
            {
                float[] samples = new float[65536];
                int count = 0;
                float[] buffer = new float[16384];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (count + read > MaxSamples) return null;
                    if (count + read > samples.Length) Array.Resize(ref samples, Math.Max(samples.Length * 2, count + read));
                    Array.Copy(buffer, 0, samples, count, read);
                    count += read;
                }
                Array.Resize(ref samples, count);
                return new CachedSound { FileName = fileName, Stamp = stamp, WaveFormat = input.WaveFormat, Samples = samples };
            }
            finally
            {
                input.Dispose();
            }
        }
    }

    /// <summary>Plays a <see cref="CachedSound"/>; every copy has its own position.</summary>
    public class MemorySampleReader : ISampleProvider
    {
        private readonly CachedSound _sound;
        private readonly bool _loop;
        private int _position;

        public WaveFormat WaveFormat { get { return _sound.WaveFormat; } }

        public MemorySampleReader(CachedSound sound, bool loop)
        {
            _sound = sound;
            _loop = loop;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            float[] samples = _sound.Samples;
            if (samples.Length == 0) return 0;

            int read = 0;
            while (read < count)
            {
                if (_position >= samples.Length)
                {
                    if (!_loop) break;
                    _position = 0;
                }
                int n = Math.Min(count - read, samples.Length - _position);
                Array.Copy(samples, _position, buffer, offset + read, n);
                _position += n;
                read += n;
            }
            return read;
        }
    }
}
