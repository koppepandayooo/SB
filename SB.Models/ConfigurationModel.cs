using GalaSoft.MvvmLight;
using SB.Utils;
using System.Xml.Serialization;

namespace SB.Models
{
    public class ConfigurationModel : ObservableObject
    {
        private int _volume;
        public int Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                RaisePropertyChanged("Volume");
            }
        }

        private int _primaryDeviceVolume;
        public int PrimaryDeviceVolume
        {
            get { return _primaryDeviceVolume; }
            set
            {
                _primaryDeviceVolume = value;
                RaisePropertyChanged("PrimaryDeviceVolume");
            }
        }

        private int _secondaryDeviceVolume;
        public int SecondaryDeviceVolume
        {
            get { return _secondaryDeviceVolume; }
            set
            {
                _secondaryDeviceVolume = value;
                RaisePropertyChanged("SecondaryDeviceVolume");
            }
        }

        private bool _overlap;
        public bool Overlap
        {
            get { return _overlap; }
            set
            {
                _overlap = value;
                RaisePropertyChanged("Overlap");
            }
        }

        private string _primaryOutput;
        public string PrimaryOutput
        {
            get { return _primaryOutput; }
            set
            {
                _primaryOutput = value;
                RaisePropertyChanged("PrimaryOutput");
            }
        }

        private string _secondaryOutput;
        public string SecondaryOutput
        {
            get { return _secondaryOutput; }
            set
            {
                _secondaryOutput = value;
                RaisePropertyChanged("SecondaryOutput");
            }
        }

        private double _windowWidth;
        public double WindowWidth
        {
            get { return _windowWidth; }
            set
            {
                _windowWidth = value;
                RaisePropertyChanged("WindowWidth");
            }
        }

        private double _windowHeight;
        public double WindowHeight
        {
            get { return _windowHeight; }
            set
            {
                _windowHeight = value;
                RaisePropertyChanged("WindowHeight");
            }
        }

        private bool _minimizeToTray;
        public bool MinimizeToTray
        {
            get { return _minimizeToTray; }
            set
            {
                _minimizeToTray = value;
                RaisePropertyChanged("MinimizeToTray");
            }
        }

        private WheelMode _wheelMode;
        public WheelMode WheelMode
        {
            get { return _wheelMode; }
            set
            {
                _wheelMode = value;
                RaisePropertyChanged("WheelMode");
            }
        }

        private string _language = "auto";
        public string Language
        {
            get { return _language; }
            set
            {
                _language = value;
                RaisePropertyChanged("Language");
            }
        }

        private int _stackCount = 1;
        /// <summary>How many copies of every sound one play starts (sounds can override this).</summary>
        public int StackCount
        {
            get { return _stackCount; }
            set
            {
                _stackCount = value;
                RaisePropertyChanged("StackCount");
            }
        }

        private int _stackOffset;
        /// <summary>Milliseconds between the start of one copy and the next.</summary>
        public int StackOffset
        {
            get { return _stackOffset; }
            set
            {
                _stackOffset = value;
                RaisePropertyChanged("StackOffset");
            }
        }

        private bool _tileView;
        public bool TileView
        {
            get { return _tileView; }
            set
            {
                _tileView = value;
                RaisePropertyChanged("TileView");
            }
        }

        private bool _showSelectionOverlay;
        public bool ShowSelectionOverlay
        {
            get { return _showSelectionOverlay; }
            set
            {
                _showSelectionOverlay = value;
                RaisePropertyChanged("ShowSelectionOverlay");
            }
        }

        // XmlSerializer appends to collections created in constructors, so default
        // key bindings are applied once after loading instead of in the constructor
        public bool WheelDefaultsApplied { get; set; }

        private ObservableObjectCollection<Preset> _presetCollection;
        public ObservableObjectCollection<Preset> PresetCollection
        {
            get { return _presetCollection; }
            set
            {
                _presetCollection = value;
                RaisePropertyChanged("PresetCollection");
            }
        }

        private int _selectedPresetIndex;
        public int SelectedPresetIndex
        {
            get { return _selectedPresetIndex; }
            set
            {
                _selectedPresetIndex = value;
                RaisePropertyChanged("SelectedPreset");
            }
        }

        [XmlIgnore]
        public Preset SelectedPreset
        {
            get { return _selectedPresetIndex < PresetCollection.Count ? PresetCollection[_selectedPresetIndex] : PresetCollection[0]; }
            set
            {
                int index = PresetCollection.IndexOf(value);
                SelectedPresetIndex = index == -1 ? 0 : index;
            }
        }

        private SoundShortcuts _soundShortcuts;
        public SoundShortcuts SoundShortcuts
        {
            get { return _soundShortcuts; }
            set
            {
                _soundShortcuts = value;
                RaisePropertyChanged("SoundShortcuts");
            }
        }

        public ConfigurationModel()
        {
            PresetCollection = new ObservableObjectCollection<Preset>();
            SoundShortcuts = new SoundShortcuts();

            PresetCollection.CollectionChanged += (sender, e) => RaisePropertyChanged("PresetCollection");
            PresetCollection.CollectionChanged += (sender, e) => RaisePropertyChanged("SelectedPreset");
            SoundShortcuts.PropertyChanged += (sender, e) => RaisePropertyChanged("SoundShortcuts");

            Volume = 100;
            PrimaryDeviceVolume = 100;
            SecondaryDeviceVolume = 100;
            WindowHeight = 300;
            WindowWidth = 500;
            WheelMode = WheelMode.WithModifier;
            ShowSelectionOverlay = true;
        }
    }
}
