using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using SB.Business;
using SB.Input;
using SB.Models;
using SB.Utils;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Principal;
using System.Diagnostics;

namespace SB.ViewModels
{
    public class ViewModel : ObservableObject
    {
        private ApplicationStateModel _applicationStateModel;
        private ConfigurationModel _configurationModel;

        private ConfigurationManager _configurationManager;
        private OpenFileManager _openFileManager;
        private ShortcutManager _shortcutManager;
        private SoundManager _soundManager;
        private UpdateManager _updateManager;
        private KeyboardInput _keyboardInput;

        private PresetConfigurationViewModel _presetConfigurationViewModel;

        private double _previousVolume;
        private double _previousPrimaryVolume;
        private double _previousSecondaryVolume;

        public ViewModel()
        {
            _applicationStateModel = new ApplicationStateModel();
            _configurationManager = new ConfigurationManager();
            _configurationModel = _configurationManager.Load();
            if (_configurationModel.PresetCollection.Count == 0) _configurationModel.PresetCollection.Add(new Preset() { Name = UiLanguage.Get("Str_NewPreset") } );
            _openFileManager = new OpenFileManager();
            _soundManager = new SoundManager(_configurationModel);
            _shortcutManager = new ShortcutManager(_applicationStateModel, _configurationModel, _soundManager);
            _updateManager = new UpdateManager();

            _presetConfigurationViewModel = new PresetConfigurationViewModel(_applicationStateModel, _configurationModel);

            _configurationModel.PropertyChanged += (sender, e) => _configurationManager.Save((ConfigurationModel)sender);

            _configurationModel.SoundShortcuts.Pause.Command = PauseCommand;
            _configurationModel.SoundShortcuts.Continue.Command = ContinueCommand;
            _configurationModel.SoundShortcuts.Stop.Command = StopCommand;

            if (!_configurationModel.WheelDefaultsApplied)
            {
                if (_configurationModel.SoundShortcuts.PlaySelected.Keys.Count == 0)
                    _configurationModel.SoundShortcuts.PlaySelected.Keys.Add(VKey.MBUTTON);
                if (_configurationModel.SoundShortcuts.WheelModifier.Keys.Count == 0)
                    _configurationModel.SoundShortcuts.WheelModifier.Keys.Add(VKey.MENU);
                _configurationModel.WheelDefaultsApplied = true;
            }

            UiLanguage.Apply(_configurationModel.Language);

            Task.Run(() => _updateManager.AutoUpdateCheck(Version));

            _shortcutManager.SelectionChangedByWheel += sound => SelectionChangedByWheel?.Invoke(sound);
        }

        public IntPtr WindowHandle
        {
            set
            {
                _keyboardInput = new KeyboardInput(value);
                _keyboardInput.KeyUp += _shortcutManager.KeyUp;
                _keyboardInput.KeyDown += _shortcutManager.KeyDown;
                _keyboardInput.KeyPress += _shortcutManager.KeyPress;
                _keyboardInput.Wheel += _shortcutManager.Wheel;
            }
        }

        /// <summary>Raised when the selected sound was changed with the mouse wheel.</summary>
        public event Action<Sound> SelectionChangedByWheel;

        public ApplicationStateModel ApplicationStateModel
        {
            get { return _applicationStateModel; }
        }

        public ConfigurationModel ConfigurationModel
        {
            get { return _configurationModel; }
        }

        public PresetConfigurationViewModel PresetConfigurationViewModel
        {
            get { return _presetConfigurationViewModel; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public double CurrentVolume
        {
            get { return _configurationModel.Volume; }
            set
            {
                _configurationModel.Volume = (int)value;
                _soundManager.Volume = _configurationModel.Volume / 100f;
                RaisePropertyChanged("CurrentVolume");
            }
        }

        public double PrimaryDeviceVolume
        {
            get { return _configurationModel.PrimaryDeviceVolume; }
            set
            {
                _configurationModel.PrimaryDeviceVolume = (int)value;
                _soundManager.PrimaryDeviceVolume = _configurationModel.PrimaryDeviceVolume / 100f;
                RaisePropertyChanged("PrimaryDeviceVolume");
            }
        }

        public double SecondaryDeviceVolume
        {
            get { return _configurationModel.SecondaryDeviceVolume; }
            set
            {
                _configurationModel.SecondaryDeviceVolume = (int)value;
                _soundManager.SecondaryDeviceVolume = _configurationModel.SecondaryDeviceVolume / 100f;
                RaisePropertyChanged("SecondaryDeviceVolume");
            }
        }

        public bool Overlap
        {
            get { return _configurationModel.Overlap; }
            set
            {
                _configurationModel.Overlap = value;
                _soundManager.Overlap = _configurationModel.Overlap;
                RaisePropertyChanged("Overlap");
            }
        }

        public string Language
        {
            get { return _configurationModel.Language; }
            set
            {
                _configurationModel.Language = value;
                UiLanguage.Apply(value);
                RaisePropertyChanged("Language");
            }
        }

        public WheelMode WheelMode
        {
            get { return _configurationModel.WheelMode; }
            set
            {
                _configurationModel.WheelMode = value;
                RaisePropertyChanged("WheelMode");
            }
        }

        public ICollection<string> AvailableOutputs
        {
            get { return _soundManager.EnumerateDevices(); }
        }

        public string PrimaryOutput
        {
            get
            {
                return _configurationModel.PrimaryOutput;
            }
            set
            {
                string selectedDeviceName = _soundManager.ChangePrimaryOutput(value);
                _configurationModel.PrimaryOutput = selectedDeviceName;
                RaisePropertyChanged("PrimaryOutput");
            }
        }

        public string SecondaryOutput
        {
            get
            {
                return _configurationModel.SecondaryOutput;
            }
            set
            {
                string selectedDeviceName = _soundManager.ChangeSecondaryOutput(value);
                _configurationModel.SecondaryOutput = selectedDeviceName;
                RaisePropertyChanged("SecondaryOutput");
            }
        }

        public Visibility NotAdministrator
        {
            get
            {
                return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator) ?
                    Visibility.Collapsed : 
                    Visibility.Visible;
            }
        }

        public ICommand CheckForUpdatesCommand
        {
            get { return new RelayCommand(CheckForUpdates); }
        }
        private async void CheckForUpdates()
        {
            await _updateManager.ManualUpdateCheck(Version);
        }

        public ICommand PresetSelectedCommand
        {
            get { return new RelayCommand<Preset>(PresetSelected); }
        }
        private void PresetSelected(Preset selectedPreset)
        {
            _configurationModel.SelectedPreset = selectedPreset;
        }

        public ICommand OpenSettingsCommand
        {
            get { return new RelayCommand(OpenSettings); }
        }
        private void OpenSettings()
        {
            _applicationStateModel.SettingsOpened = true;
        }

        public ICommand OpenSoundCommand
        {
            get { return new RelayCommand(OpenSound); }
        }
        private void OpenSound()
        {
            if (_configurationModel.SelectedPreset.SelectedSound != null)
            {
                _applicationStateModel.ModifiedSound = _configurationModel.SelectedPreset.SelectedSound;
                _applicationStateModel.SoundOpened = true;
            }
        }

        public ICommand OpenAboutCommand
        {
            get { return new RelayCommand(OpenAbout); }
        }
        private void OpenAbout()
        {
            _applicationStateModel.AboutOpened = true;
        }

        public ICommand OpenNotAdministratorCommand
        {
            get { return new RelayCommand(OpenNotAdministrator); }
        }
        private void OpenNotAdministrator()
        {
            var result = MessageBox.Show(UiLanguage.Get("Str_NotAdminMessage"),
                UiLanguage.Get("Str_NotAdmin"),
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                var startInfo = new ProcessStartInfo(exeName) { Verb = "runas" };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
        }

        public ICommand OpenSoundFileDialogCommand
        {
            get { return new RelayCommand(OpenSoundFileDialog); }
        }
        private void OpenSoundFileDialog()
        {
            string[] result = _openFileManager.OpenSoundFiles();
            if (result != null)
            {
                _configurationModel.SelectedPreset.SelectedSound.Files.Clear();
                foreach (string file in result)
                {
                    _configurationModel.SelectedPreset.SelectedSound.Files.Add(file);
                }
            }
        }

        public ICommand MuteCommand
        {
            get { return new RelayCommand(Mute); }
        }
        private void Mute()
        {
            if (CurrentVolume == 0)
            {
                CurrentVolume = _previousVolume;
            }
            else
            {
                _previousVolume = CurrentVolume;
                CurrentVolume = 0;
            }
        }

        public ICommand MutePrimaryCommand
        {
            get { return new RelayCommand(MutePrimary); }
        }
        private void MutePrimary()
        {
            if (PrimaryDeviceVolume == 0)
            {
                PrimaryDeviceVolume = _previousPrimaryVolume;
            }
            else
            {
                _previousPrimaryVolume = PrimaryDeviceVolume;
                PrimaryDeviceVolume = 0;
            }
        }

        public ICommand MuteSecondaryCommand
        {
            get { return new RelayCommand(MuteSecondary); }
        }
        private void MuteSecondary()
        {
            if (SecondaryDeviceVolume == 0)
            {
                SecondaryDeviceVolume = _previousSecondaryVolume;
            }
            else
            {
                _previousSecondaryVolume = SecondaryDeviceVolume;
                SecondaryDeviceVolume = 0;
            }
        }

        public ICommand AddSoundCommand
        {
            get { return new RelayCommand(AddSound); }
        }
        private void AddSound()
        {
            Sound sound = new Sound();
            _configurationModel.SelectedPreset.SelectedSound = sound;
            _configurationModel.SelectedPreset.SoundCollection.Add(sound);
            _applicationStateModel.ModifiedSound = sound;
            _applicationStateModel.SoundOpened = true;
        }

        /// <summary>
        /// Adds one sound per audio file. Folders are searched recursively.
        /// Returns the number of sounds added.
        /// </summary>
        public int AddSoundFiles(IEnumerable<string> paths)
        {
            Sound lastAdded = null;
            int added = 0;
            foreach (string file in ExpandSoundFiles(paths))
            {
                Sound sound = new Sound() { Name = Path.GetFileNameWithoutExtension(file) };
                sound.Files.Add(file);
                _configurationModel.SelectedPreset.SoundCollection.Add(sound);
                lastAdded = sound;
                added++;
            }
            if (lastAdded != null)
            {
                _configurationModel.SelectedPreset.SelectedSound = lastAdded;
            }
            return added;
        }

        private static IEnumerable<string> ExpandSoundFiles(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                IEnumerable<string> files;
                if (Directory.Exists(path))
                    files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                else if (File.Exists(path))
                    files = new[] { path };
                else
                    continue;

                foreach (string file in files)
                {
                    if (OpenFileManager.SoundExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        yield return file;
                }
            }
        }

        public ICommand RemoveSoundCommand
        {
            get { return new RelayCommand(RemoveSound); }
        }
        private void RemoveSound()
        {
            _configurationModel.SelectedPreset.SoundCollection.Remove(_configurationModel.SelectedPreset.SelectedSound);
        }

        public ICommand PlayCommand
        {
            get { return new RelayCommand(Play); }
        }
        private void Play()
        {
            if (_configurationModel.SelectedPreset.SelectedSound != null)
            {
                _soundManager.Play(_configurationModel.SelectedPreset.SelectedSound);
            }
        }

        public ICommand PauseCommand
        {
            get { return new RelayCommand(Pause); }
        }
        private void Pause()
        {
            _soundManager.Pause();
        }

        public ICommand ContinueCommand
        {
            get { return new RelayCommand(Continue); }
        }
        private void Continue()
        {
            _soundManager.Continue();
        }

        public ICommand StopCommand
        {
            get { return new RelayCommand(Stop); }
        }
        private void Stop()
        {
            _soundManager.Stop();
        }

        public ICommand BindKeysCommand
        {
            get { return new RelayCommand<IBindable>(BindKeys); }
        }
        public void BindKeys(IBindable bindable)
        {
            _applicationStateModel.ModifiedBindable = bindable;
            _applicationStateModel.BindKeysOpened = true;
        }

        public ICommand CancelBindKeysCommand
        {
            get { return new RelayCommand(CancelBindKeys); }
        }
        public void CancelBindKeys()
        {
            _applicationStateModel.BindKeysOpened = false;
            _applicationStateModel.ModifiedBindable = null;
        }

        public ICommand ClearKeysCommand
        {
            get { return new RelayCommand(ClearKeys); }
        }
        private void ClearKeys()
        {
            _applicationStateModel.BindKeysOpened = false;
            _applicationStateModel.ModifiedBindable.Keys.Clear();
            _applicationStateModel.ModifiedBindable = null;
        }

        public ICommand ClosingCommand
        {
            get { return new RelayCommand(Closing); }
        }
        private void Closing()
        {
            _configurationManager.Dispose();
        }
    }
}
