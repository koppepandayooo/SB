using System.Windows.Controls;

namespace DCSB.Views.MainWindow
{
    /// <summary>
    /// Interaction logic for SoundListView.xaml
    /// </summary>
    public partial class SoundListView : UserControl
    {
        public SoundListView()
        {
            InitializeComponent();
        }

        private void SoundGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The selection may change from the mouse wheel while the window is in the background
            if (SoundGrid.SelectedItem != null)
                SoundGrid.ScrollIntoView(SoundGrid.SelectedItem);
        }
    }
}
