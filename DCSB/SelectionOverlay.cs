using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DCSB
{
    /// <summary>
    /// Small always-on-top, click-through popup that briefly shows the selected sound's name.
    /// Visible over borderless/windowed games (not exclusive fullscreen).
    /// </summary>
    public class SelectionOverlay : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private readonly TextBlock _text;
        private readonly DispatcherTimer _hideTimer;

        public SelectionOverlay()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            _text = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold
            };
            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x10, 0x10, 0x10)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 8, 14, 8),
                Child = _text
            };

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            _hideTimer.Tick += (sender, e) =>
            {
                _hideTimer.Stop();
                Hide();
            };

            SourceInitialized += (sender, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            };
            SizeChanged += (sender, e) => Reposition();
        }

        public void ShowMessage(string message)
        {
            _text.Text = message;
            if (!IsVisible) Show();
            Topmost = true;
            Reposition();
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void Reposition()
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Left + (area.Width - ActualWidth) / 2;
            Top = area.Top + area.Height * 0.7;
        }
    }
}
