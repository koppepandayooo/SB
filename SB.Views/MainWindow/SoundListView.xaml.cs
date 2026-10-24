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

            // A new preset has a new collection (and a new view), so apply the chosen sort to it again
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox))
                .AddValueChanged(SoundTiles, (sender, e) =>
                {
                    if (TileHost.Visibility == Visibility.Visible) ApplySort();
                });

            // Sorting by a column header in the list view changes the same view; show it in the sort box
            TileHost.IsVisibleChanged += (sender, e) =>
            {
                if (TileHost.Visibility == Visibility.Visible) SyncSortCombo();
            };
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
            if (!_syncingSortCombo && SoundTiles != null)
                ApplySort();
        }

        private void ApplySort()
        {
            ICollectionView view = SoundTiles.ItemsSource == null ? null : CollectionViewSource.GetDefaultView(SoundTiles.ItemsSource);
            if (view == null) return;

            string tag = (SortCombo.SelectedItem as ComboBoxItem)?.Tag as string;
            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                if (tag == "asc")
                    view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                else if (tag == "desc")
                    view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
            }
        }

        private void SyncSortCombo()
        {
            ICollectionView view = SoundTiles.ItemsSource == null ? null : CollectionViewSource.GetDefaultView(SoundTiles.ItemsSource);
            string tag = "none";
            if (view != null && view.SortDescriptions.Count == 1 && view.SortDescriptions[0].PropertyName == "Name")
                tag = view.SortDescriptions[0].Direction == ListSortDirection.Ascending ? "asc" : "desc";

            _syncingSortCombo = true;
            SortCombo.SelectedItem = SortCombo.Items.Cast<ComboBoxItem>().First(x => (string)x.Tag == tag);
            _syncingSortCombo = false;
        }
    }
}
