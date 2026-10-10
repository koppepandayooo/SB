using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Xml.Serialization;
using SB.Utils;
using GalaSoft.MvvmLight;

namespace SB.Models
{
    public class Shortcut : ObservableObject, IBindable
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

        [XmlIgnore]
        public ICommand Command { get; set; }

        public Shortcut()
        {
            _keys = new ObservableCollection<VKey>();

            _keys.CollectionChanged += (sender, e) => RaisePropertyChanged("Keys");
        }
    }
}
