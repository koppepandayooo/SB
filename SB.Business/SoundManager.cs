using SB.Models;
using SB.SoundPlayer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SB.Business
{
    public class SoundManager
    {
        private AudioPlaybackEngine _primarySoundPlayer;
        private AudioPlaybackEngine _secondarySoundPlayer;
        private Random _random;

        private float _volume = 1f;
        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (_primarySoundPlayer != null) _primarySoundPlayer.Volume = _volume * _primaryDeviceVolume;
                if (_secondarySoundPlayer != null) _secondarySoundPlayer.Volume = _volume * _secondaryDeviceVolume;
            }
        }

        private float _primaryDeviceVolume = 1f;
        public float PrimaryDeviceVolume
        {
            get { return _primaryDeviceVolume; }
            set
            {
                _primaryDeviceVolume = value;
                if (_primarySoundPlayer != null) _primarySoundPlayer.Volume = _volume * _primaryDeviceVolume;
            }
        }

        private float _secondaryDeviceVolume = 1f;
        public float SecondaryDeviceVolume
        {
            get { return _secondaryDeviceVolume; }
            set
            {
                _secondaryDeviceVolume = value;
                if (_secondarySoundPlayer != null) _secondarySoundPlayer.Volume = _volume * _secondaryDeviceVolume;
            }
        }

        private bool _overlap;
        public bool Overlap
        {
            get { return _overlap; }
            set
            {
                _overlap = value;
                if (_primarySoundPlayer != null) _primarySoundPlayer.Overlap = value;
                if (_secondarySoundPlayer != null) _secondarySoundPlayer.Overlap = value;
            }
        }

        public SoundManager(ConfigurationModel configurationModel)
        {
            _random = new Random();

            configurationModel.PrimaryOutput = ChangePrimaryOutput(configurationModel.PrimaryOutput);
            configurationModel.SecondaryOutput = ChangeSecondaryOutput(configurationModel.SecondaryOutput);

            Volume = configurationModel.Volume / 100f;
            PrimaryDeviceVolume = configurationModel.PrimaryDeviceVolume / 100f;
            SecondaryDeviceVolume = configurationModel.SecondaryDeviceVolume / 100f;
            Overlap = configurationModel.Overlap;
            StackCount = configurationModel.StackCount;
            StackOffset = configurationModel.StackOffset;
            StackJitter = configurationModel.StackJitter;
            PlaybackSpeed = configurationModel.PlaybackSpeed;
            Pitch = configurationModel.Pitch;
        }

        public void Play(Sound sound)
        {
            if (sound.Files.Count == 0)
            {
                return;
            }

            string file = sound.Files[_random.Next(sound.Files.Count)];
            float volume = sound.Volume / 100f;
            bool loop = sound.Loop;
            // A sound with its own stack setting (> 0) uses it; 0 means "use the app setting"
            bool own = sound.StackCount > 0;
            int copies = Math.Max(1, Math.Min(MaxCopies, own ? sound.StackCount : StackCount));
            int offset = Math.Max(0, own ? sound.StackOffset : StackOffset);

            // With several copies, decoding the file once is quicker than opening it for every copy
            bool shared = copies >= SharedFromCopies;

            // The copies are all started now; each one has its own start offset inside the mixer
            // (a regular step plus a random part, so they do not add up to one louder copy of the same waveform)
            // Without overlapping, the first copy replaces what is playing, including copies that have not started yet
            for (int i = 0; i < copies; i++)
            {
                int start = i == 0 ? 0 : i * offset + (StackJitter > 0 ? _random.Next(0, StackJitter + 1) : 0);
                try
                {
                    PlayOnDevices(file, volume, loop, i > 0, shared, start);
                }
                catch (Exception ex)
                {
                    // Only the first copy reports a problem with the file
                    if (i == 0)
                    {
                        sound.Error = ex.ToString();
                        return;
                    }
                }
            }
        }

        public const int MaxCopies = 50;
        private const int SharedFromCopies = 2;

        /// <summary>App setting: copies started by one play (for sounds without their own setting).</summary>
        public int StackCount { get; set; } = 1;

        /// <summary>App setting: milliseconds between copies.</summary>
        public int StackOffset { get; set; }

        /// <summary>App setting: each copy also starts up to this many milliseconds late, at random.</summary>
        public int StackJitter { get; set; }

        /// <summary>App setting: playback speed in percent (100 = normal), without changing the pitch.</summary>
        public int PlaybackSpeed { get; set; } = 100;

        /// <summary>App setting: pitch change in semitones (0 = normal), without changing the speed.</summary>
        public int Pitch { get; set; }

        private void PlayOnDevices(string file, float volume, bool loop, bool addToPlaying, bool shared, int delayMs)
        {
            float speed = Math.Max(50, Math.Min(200, PlaybackSpeed)) / 100f;
            float pitch = Math.Max(-12, Math.Min(12, Pitch));
            if (_primarySoundPlayer != null) _primarySoundPlayer.PlaySound(file, volume, loop, addToPlaying, shared, delayMs, speed, pitch);
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.PlaySound(file, volume, loop, addToPlaying, shared, delayMs, speed, pitch);
        }

        public void Pause()
        {
            if (_primarySoundPlayer != null) _primarySoundPlayer.Pause();
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.Pause();
        }

        public void Continue()
        {
            if (_primarySoundPlayer != null) _primarySoundPlayer.Continue();
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.Continue();
        }

        public void Stop()
        {
            if (_primarySoundPlayer != null) _primarySoundPlayer.Stop();
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.Stop();
        }

        private string InstantiateDevice(string deviceName, bool primary, ref AudioPlaybackEngine soundPlayer)
        {
            KeyValuePair<int, string> device = GetDevice(deviceName, primary);

            if (device.Equals(default(KeyValuePair<int, string>)) || device.Value == "Disabled")
            {
                soundPlayer = null;
                return "Disabled";
            }

            soundPlayer = new AudioPlaybackEngine(device.Key);
            return device.Value;
        }

        private string ChangeDevice(string deviceName, float deviceVolume, ref AudioPlaybackEngine soundPlayer)
        {
            if (soundPlayer != null)
            {
                soundPlayer.Stop();
                soundPlayer.Dispose();
            }

            string selectedDeviceName = InstantiateDevice(deviceName, soundPlayer == _primarySoundPlayer, ref soundPlayer);

            if (soundPlayer != null)
            {
                soundPlayer.Overlap = Overlap;
                soundPlayer.Volume = _volume * deviceVolume;
            }

            return selectedDeviceName;
        }

        public string ChangePrimaryOutput(string deviceName)
        {
            return ChangeDevice(deviceName, _primaryDeviceVolume, ref _primarySoundPlayer);
        }

        public string ChangeSecondaryOutput(string deviceName)
        {
            return ChangeDevice(deviceName, _secondaryDeviceVolume, ref _secondarySoundPlayer);
        }

        public ICollection<string> EnumerateDevices()
        {
            return AudioPlaybackEngine.EnumerateDevices().Values;
        }

        public KeyValuePair<int, string> GetDevice(string name, bool primary)
        {
            IDictionary<int, string> devices = AudioPlaybackEngine.EnumerateDevices();
            KeyValuePair<int, string> device = devices.Where(x => x.Value == name).FirstOrDefault();

            if (primary && device.Equals(default(KeyValuePair<int, string>)) && devices.Count > 2)
            {
                return new KeyValuePair<int, string>(-1, devices[-1]);
            }

            return device;
        }

        ~SoundManager()
        {
            if (_primarySoundPlayer != null) _primarySoundPlayer.Dispose();
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.Dispose();
        }
    }
}
