using SB.Models;
using SB.SoundPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            int delay = Math.Max(0, own ? sound.StackOffset : StackOffset);

            // Without overlapping, a new sound replaces the old one, including copies still waiting to start
            if (!_overlap) CancelPendingCopies();
            CancellationToken token = CurrentCopiesToken();

            try
            {
                PlayOnDevices(file, volume, loop, false);
            }
            catch (Exception ex)
            {
                sound.Error = ex.ToString();
                return;
            }

            // The extra copies play the same file; all at once, or one after another every "delay" ms
            for (int i = 1; i < copies; i++)
            {
                if (delay == 0)
                {
                    TryPlayCopy(file, volume, loop);
                }
                else
                {
                    Task.Delay(i * delay, token).ContinueWith(
                        t => { if (!t.IsCanceled) TryPlayCopy(file, volume, loop); },
                        TaskScheduler.Default);
                }
            }
        }

        public const int MaxCopies = 10;

        /// <summary>App setting: copies started by one play (for sounds without their own setting).</summary>
        public int StackCount { get; set; } = 1;

        /// <summary>App setting: milliseconds between copies.</summary>
        public int StackOffset { get; set; }

        private CancellationTokenSource _copiesCts = new CancellationTokenSource();
        private readonly object _copiesLock = new object();

        private void PlayOnDevices(string file, float volume, bool loop, bool addToPlaying)
        {
            if (_primarySoundPlayer != null) _primarySoundPlayer.PlaySound(file, volume, loop, addToPlaying);
            if (_secondarySoundPlayer != null) _secondarySoundPlayer.PlaySound(file, volume, loop, addToPlaying);
        }

        private void TryPlayCopy(string file, float volume, bool loop)
        {
            // The first copy already reported any problem with the file, and this may run on another thread
            try { PlayOnDevices(file, volume, loop, true); }
            catch (Exception) { }
        }

        private CancellationToken CurrentCopiesToken()
        {
            lock (_copiesLock) return _copiesCts.Token;
        }

        private void CancelPendingCopies()
        {
            lock (_copiesLock)
            {
                _copiesCts.Cancel();
                _copiesCts = new CancellationTokenSource();
            }
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
            CancelPendingCopies();
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
