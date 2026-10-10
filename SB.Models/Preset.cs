using GalaSoft.MvvmLight;
using SB.Utils;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System;
using System.Linq;

namespace SB.Models
{
    public class Preset : ObservableObject, IBindable, ICloneable
    {
        private ObservableCollection<VKey> _keys;
        public ObservableCollection<VKey> Keys
        {
            get { return _keys; }
            set
            {
                _keys = value;
                RaisePropertyChanged("Keys");
            }
        }

        private ObservableObjectCollection<Sound> _soundCollection;
        public ObservableObjectCollection<Sound> SoundCollection
        {
            get { return _soundCollection; }
            set
            {
                _soundCollection = value;
                RaisePropertyChanged("SoundCollection");
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        private Sound _selectedSound;
        [XmlIgnore]
        public Sound SelectedSound
        {
            get { return _selectedSound; }
            set
            {
                _selectedSound = value;
                RaisePropertyChanged("SelectedSound");
            }
        }

        public Preset()
        {
            _keys = new ObservableCollection<VKey>();
            _soundCollection = new ObservableObjectCollection<Sound>();

            Keys.CollectionChanged += (sender, e) => RaisePropertyChanged("Keys");
            SoundCollection.CollectionChanged += (sender, e) => RaisePropertyChanged("SoundCollection");
            SoundCollection.CollectionChanged += (sender, e) => RaisePropertyChanged("SelectedSound");
        }

        public object Clone()
        {
            Preset clonedPreset = new Preset() { Name = $"{Name} copy" };
            foreach (VKey key in Keys) clonedPreset.Keys.Add(key);
            foreach (Sound sound in SoundCollection) clonedPreset.SoundCollection.Add((Sound)sound.Clone());
            return clonedPreset;
        }
    }
}
