using SB.Models;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SB.SoundPlayer
{
    public class AudioPlaybackEngine
    {
        private readonly WaveOutEvent _outputDevice;
        private readonly MixingSampleProvider _mixer;

        private int _volumePowBase = 100;

        public float Volume
        {
            get { return RevertVolume(_outputDevice.Volume); }
            set { _outputDevice.Volume = AdjustVolume(value); }
        }

        public bool Overlap { get; set; }

        public AudioPlaybackEngine(int deviceNumber)
        {
            _outputDevice = new WaveOutEvent() { DeviceNumber = deviceNumber };
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        /// <param name="speed">Playback speed (1 = normal); the pitch is not changed by it.</param>
        /// <param name="pitchSemitones">Pitch change in semitones (0 = normal); the speed is not changed by it.</param>
        /// <param name="delayMs">Start this long after now, counted in the mixer itself.</param>
        /// <param name="shared">Several copies of the sound are started together: decode it once and let the copies share it.</param>
        /// <param name="addToPlaying">Add to what is playing even when overlapping is off (used for extra copies of one sound).</param>
        public void PlaySound(string fileName, float volume, bool loop, bool addToPlaying = false, bool shared = false, int delayMs = 0, float speed = 1f, float pitchSemitones = 0f)
        {
            if (!Overlap && !addToPlaying)
            {
                Stop();
            }

            // Opening and decoding the file takes several ms for each copy, so many copies would start late
            CachedSound cached = shared ? CachedSound.Get(fileName) : null;
            if (cached != null)
            {
                AddMixerInput(new MemorySampleReader(cached, loop), volume, delayMs, speed, pitchSemitones);
                if (_outputDevice.PlaybackState != PlaybackState.Playing)
                {
                    _outputDevice.Play();
                }
                return;
            }

            IAudioReader input;
            try
            {
                input = new FileReader(fileName);
            }
            catch (COMException)
            {
                input = new OggFileReader(fileName);
            }

            SampleReader reader = new SampleReader(input, loop);
            AddMixerInput(reader, volume, delayMs, speed, pitchSemitones);

            if (_outputDevice.PlaybackState != PlaybackState.Playing)
            {
                _outputDevice.Play();
            }
        }

        public void Pause()
        {
            if (_outputDevice.PlaybackState == PlaybackState.Playing)
            {
                _outputDevice.Pause();
            }
        }

        public void Continue()
        {
            if (_outputDevice.PlaybackState == PlaybackState.Paused)
            {
                _outputDevice.Play();
            }
        }

        public void Stop()
        {
            _mixer.RemoveAllMixerInputs();
            _outputDevice.Stop();
            if (EnumerateDevices().ContainsKey(_outputDevice.DeviceNumber))
                _outputDevice.Init(_mixer);
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException($"Channel conversion from {input.WaveFormat.Channels} to {_mixer.WaveFormat.Channels} is not supported.");
        }

        private ISampleProvider ConvertToRightSampleRate(ISampleProvider input)
        {
            if (input.WaveFormat.SampleRate == _mixer.WaveFormat.SampleRate)
            {
                return input;
            }
            return new WdlResamplingSampleProvider(input, _mixer.WaveFormat.SampleRate);
        }

        private void AddMixerInput(ISampleProvider input, float volume, int delayMs = 0, float speed = 1f, float pitchSemitones = 0f)
        {
            ISampleProvider convertedInput = ConvertToRightSampleRate(ConvertToRightChannelCount(input));

            // Resampling alone changes speed and pitch together; the pitch shifter then moves the pitch to where it
            // should be, so speed and pitch can be set independently
            if (Math.Abs(speed - 1f) > 0.001f)
            {
                convertedInput = new WdlResamplingSampleProvider(new RateSampleProvider(convertedInput, speed), _mixer.WaveFormat.SampleRate);
            }
            float pitchFactor = (float)Math.Pow(2.0, pitchSemitones / 12.0) / speed;
            if (Math.Abs(pitchFactor - 1f) > 0.001f)
            {
                convertedInput = new SmbPitchShiftingSampleProvider(convertedInput, 2048, 4, pitchFactor);
            }
            if (delayMs > 0)
            {
                convertedInput = new OffsetSampleProvider(convertedInput) { DelayBy = TimeSpan.FromMilliseconds(delayMs) };
            }
            VolumeSampleProvider volumeSampleProvider = new VolumeSampleProvider(convertedInput) { Volume = AdjustVolume(volume) };
            _mixer.AddMixerInput(volumeSampleProvider);
        }

        private float AdjustVolume(float volume)
        {
            return (float)((Math.Pow(_volumePowBase, volume) - 1) / (_volumePowBase - 1));
        }

        private int RevertVolume(float volume)
        {
            return (int)(Math.Log(volume * (_volumePowBase - 1) + 1) / Math.Log(_volumePowBase));
        }

        public static IDictionary<int, string> EnumerateDevices()
        {
            Dictionary<int, string> devices = new Dictionary<int, string>
            {
                [-2] = "Disabled"
            };

            if (WaveOut.DeviceCount != 0)
            {
                devices[-1] = "Default Output Device";

                for (int n = 0; n < WaveOut.DeviceCount; n++)
                {
                    devices[n] = WaveOut.GetCapabilities(n).ProductName;
                }
            }

            return devices;
        }

        public void Dispose()
        {
            _outputDevice.Dispose();
        }
    }
}
