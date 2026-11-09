using NAudio.Wave;

namespace SB.SoundPlayer
{
    /// <summary>
    /// Passes samples through unchanged but claims a different sample rate; a resampler after it then
    /// plays the sound faster or slower (and higher or lower) by that factor.
    /// </summary>
    public class RateSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;

        public WaveFormat WaveFormat { get; private set; }

        public RateSampleProvider(ISampleProvider source, float speed)
        {
            _source = source;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)(source.WaveFormat.SampleRate * speed), source.WaveFormat.Channels);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _source.Read(buffer, offset, count);
        }
    }
}
