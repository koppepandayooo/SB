using System.Windows.Controls;
using SB.Models;
using SB.ViewModels;

namespace SB.Views.MainWindow
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

            // Several sounds can be selected (Ctrl/Shift+click, Ctrl+A) to remove them together
            if (DataContext is ViewModel viewModel)
            {
                viewModel.SelectedSounds.Clear();
                foreach (Sound sound in SoundGrid.SelectedItems)
                    viewModel.SelectedSounds.Add(sound);
            }
        }
    }
}
