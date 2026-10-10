using GalaSoft.MvvmLight;

namespace DCSB.Models
{
    public class ApplicationStateModel : ObservableObject
    {
        private bool _settingsOpened;
        public bool SettingsOpened
        {
            get { return _settingsOpened; }
            set
            {
                _settingsOpened = value;
                RaisePropertyChanged("SettingsOpened");
            }
        }

        private bool _soundOpened;
        public bool SoundOpened
        {
            get { return _soundOpened; }
            set
            {
                _soundOpened = value;
                RaisePropertyChanged("SoundOpened");
            }
        }

        private bool _bindKeysOpened;
        public bool BindKeysOpened
        {
            get { return _bindKeysOpened; }
            set
            {
                _bindKeysOpened = value;
                RaisePropertyChanged("BindKeysOpened");
            }
        }

        private bool _aboutOpened;
        public bool AboutOpened
        {
            get { return _aboutOpened; }
            set
            {
                _aboutOpened = value;
                RaisePropertyChanged("AboutOpened");
            }
        }

        private IBindable _modifiedBindable;
        public IBindable ModifiedBindable
        {
            get { return _modifiedBindable; }
            set
            {
                _modifiedBindable = value;
                RaisePropertyChanged("ModifiedBindable");
            }
        }

        private Sound _modifiedSound;
        public Sound ModifiedSound
        {
            get { return _modifiedSound; }
            set
            {
                _modifiedSound = value;
                RaisePropertyChanged("ModifiedSound");
            }
        }
    }
}
