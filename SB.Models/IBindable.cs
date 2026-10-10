using SB.Utils;
using System.Collections.ObjectModel;

namespace SB.Models
{
    public interface IBindable
    {
        ObservableCollection<VKey> Keys { get; set; }
    }
}
