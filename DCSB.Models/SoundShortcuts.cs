using GalaSoft.MvvmLight;

namespace DCSB.Models
{
    public class SoundShortcuts : ObservableObject
    {
        private Shortcut _pause;
        public Shortcut Pause
        {
            get { return _pause; }
            set
            {
                _pause = value;
                RaisePropertyChanged("Pause");
            }
        }

        private Shortcut _continue;
        public Shortcut Continue
        {
            get { return _continue; }
            set
            {
                _continue = value;
                RaisePropertyChanged("Continue");
            }
        }

        private Shortcut _stop;
        public Shortcut Stop
        {
            get { return _stop; }
            set
            {
                _stop = value;
                RaisePropertyChanged("Stop");
            }
        }

        private Shortcut _playSelected;
        public Shortcut PlaySelected
        {
            get { return _playSelected; }
            set
            {
                _playSelected = value;
                RaisePropertyChanged("PlaySelected");
            }
        }

        private Shortcut _wheelModifier;
        public Shortcut WheelModifier
        {
            get { return _wheelModifier; }
            set
            {
                _wheelModifier = value;
                RaisePropertyChanged("WheelModifier");
            }
        }

        public SoundShortcuts()
        {
            _pause = new Shortcut();
            _continue = new Shortcut();
            _stop = new Shortcut();
            _playSelected = new Shortcut();
            _wheelModifier = new Shortcut();

            _pause.PropertyChanged += (sender, e) => RaisePropertyChanged("Pause");
            _continue.PropertyChanged += (sender, e) => RaisePropertyChanged("Continue");
            _stop.PropertyChanged += (sender, e) => RaisePropertyChanged("Stop");
            _playSelected.PropertyChanged += (sender, e) => RaisePropertyChanged("PlaySelected");
            _wheelModifier.PropertyChanged += (sender, e) => RaisePropertyChanged("WheelModifier");
        }
    }
}
