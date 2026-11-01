using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SB.Models;
using SB.ViewModels;

namespace SB.Views.MainWindow
{
    /// <summary>
    /// Interaction logic for SoundListView.xaml
    /// </summary>
    public partial class SoundListView : UserControl
    {
        private bool _syncingSortCombo;

        // One sort order shared by every preset: "none" (added order), "asc" or "desc"; kept in the configuration
        private string _sortTag = "none";

        public SoundListView()
        {
            InitializeComponent();

            // The columns are side by side, so the wheel over the list scrolls sideways
            SoundTiles.PreviewMouseWheel += (sender, e) =>
            {
                ScrollViewer scroll = FindScrollViewer(SoundTiles);
                if (scroll == null) return;
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - e.Delta);
                e.Handled = true;
            };

            // A new preset has a new collection (and a new view), so apply the shared sort to it
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox))
                .AddValueChanged(SoundTiles, (sender, e) => ApplySort());
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
                .AddValueChanged(SoundGrid, (sender, e) => ApplySort());

            // Take the saved sort order once the configuration is available
            DataContextChanged += (sender, e) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    _sortTag = viewModel.SortMode ?? "none";
                    if (SortCombo.Items.Cast<ComboBoxItem>().All(x => (string)x.Tag != _sortTag)) _sortTag = "none";
                    SyncSortCombo();
                    ApplySort();
                }
            };

            SyncSortCombo();
        }

        private void SaveSort()
        {
            if (DataContext is ViewModel viewModel) viewModel.SortMode = _sortTag;
        }

        // Clicking the Name header cycles the shared sort (added order -> A-Z -> Z-A) instead of sorting only this preset
        private void SoundGridSorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            if (e.Column.Header as string != "Name") return;

            _sortTag = _sortTag == "none" ? "asc" : _sortTag == "asc" ? "desc" : "none";
            SyncSortCombo();
            ApplySort();
            SaveSort();
        }

        private void SoundGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Both views are bound to the same selected sound; only the one that is shown reports selections
            bool tiles = TileHost.Visibility == Visibility.Visible;
            if (sender != (tiles ? (object)SoundTiles : SoundGrid))
                return;

            IList selected = tiles ? SoundTiles.SelectedItems : SoundGrid.SelectedItems;
            object current = tiles ? SoundTiles.SelectedItem : SoundGrid.SelectedItem;

            // The selection may change from the mouse wheel while the window is in the background
            if (current != null)
            {
                if (tiles) SoundTiles.ScrollIntoView(current);
                else SoundGrid.ScrollIntoView(current);
            }

            // Several sounds can be selected (Ctrl/Shift+click, Ctrl+A) to remove them together
            if (DataContext is ViewModel viewModel)
            {
                viewModel.SelectedSounds.Clear();
                foreach (Sound sound in selected)
                    viewModel.SelectedSounds.Add(sound);
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                ScrollViewer found = child as ScrollViewer ?? FindScrollViewer(child);
                if (found != null) return found;
            }
            return null;
        }

        private void TileRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right-click on a tile that is not selected selects it first, so the menu acts on what was clicked
            if (sender is ListBoxItem item && !item.IsSelected)
            {
                SoundTiles.SelectedItems.Clear();
                item.IsSelected = true;
            }
        }

        private void SortComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Fires while the XAML is still being loaded (initial selection), before the list exists
            if (_syncingSortCombo || SoundTiles == null) return;
            _sortTag = (SortCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "none";
            ApplySort();
            SaveSort();
        }

        private void ApplySort()
        {
            foreach (object source in new object[] { SoundTiles.ItemsSource, SoundGrid.ItemsSource })
            {
                ICollectionView view = source == null ? null : CollectionViewSource.GetDefaultView(source);
                if (view == null) continue;

                using (view.DeferRefresh())
                {
                    view.SortDescriptions.Clear();
                    if (_sortTag == "asc")
                        view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    else if (_sortTag == "desc")
                        view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
                }
            }

            DataGridColumn nameColumn = SoundGrid.Columns.FirstOrDefault(c => c.Header as string == "Name");
            if (nameColumn != null)
                nameColumn.SortDirection = _sortTag == "asc" ? ListSortDirection.Ascending
                    : _sortTag == "desc" ? ListSortDirection.Descending : (ListSortDirection?)null;
        }

        private void SyncSortCombo()
        {
            _syncingSortCombo = true;
            SortCombo.SelectedItem = SortCombo.Items.Cast<ComboBoxItem>().First(x => (string)x.Tag == _sortTag);
            _syncingSortCombo = false;
        }
    }
}
