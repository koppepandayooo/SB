using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using DCSB.Utils;
using DCSB.ViewModels;

namespace DCSB
{
    public partial class MainWindow : Window
    {
        NotifyIcon notifyIcon;
        SelectionOverlay selectionOverlay;

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ViewModel viewModel = new ViewModel();
            DataContext = viewModel;
            viewModel.SelectionChangedByWheel += ShowSelection;

            Icon icon;
            using (Stream stream = System.Windows.Application.GetResourceStream(new Uri("icon.ico", UriKind.Relative)).Stream)
            {
                icon = new Icon(stream);
            }

            MenuItem open = new MenuItem(UiLanguage.Get("Str_TrayOpen"), (sender, e) => Open());
            MenuItem exit = new MenuItem(UiLanguage.Get("Str_TrayExit"), (sender, e) => Close());
            UiLanguage.LanguageChanged += () =>
            {
                open.Text = UiLanguage.Get("Str_TrayOpen");
                exit.Text = UiLanguage.Get("Str_TrayExit");
            };

            notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Text = "DCSB Soundboard",
                Visible = true,
                ContextMenu = new ContextMenu(new MenuItem[] { open, exit })
            };

            notifyIcon.Click += (sender, args) => Open();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled exception");
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (DataContext is ViewModel viewModel && viewModel.ConfigurationModel.MinimizeToTray && WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        private void ShowSelection(Models.Sound sound)
        {
            if (!(DataContext is ViewModel viewModel) || !viewModel.ConfigurationModel.ShowSelectionOverlay)
                return;

            var sounds = viewModel.ConfigurationModel.SelectedPreset.SoundCollection;
            if (selectionOverlay == null)
                selectionOverlay = new SelectionOverlay();
            selectionOverlay.ShowMessage($"{sounds.IndexOf(sound) + 1}/{sounds.Count}  {sound.Name}");
        }

        private void WindowDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void WindowDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is ViewModel viewModel && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths)
            {
                if (viewModel.AddSoundFiles(paths) == 0)
                {
                    System.Windows.MessageBox.Show(this, "対応している音声ファイルが見つかりませんでした。\n(wma, mp3, wav, ogg, m4a, aiff, flac)", "DCSB");
                }
            }
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (selectionOverlay != null)
                selectionOverlay.Close();

            if (notifyIcon != null)
                notifyIcon.Dispose();

            base.OnClosed(e);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.WindowHandle = new WindowInteropHelper(this).Handle;
            }
        }

        private void Open()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
