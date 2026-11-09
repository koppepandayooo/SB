using Octokit;
using SB.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SB.Business
{
    /// <summary>
    /// Tells the user when a newer release is published on GitHub and, if wanted, downloads its installer
    /// and runs it. The installer asks for administrator rights itself and starts the new version when done.
    /// </summary>
    internal static class DispatcherFrameHelper
    {
        /// <summary>Waits for a condition while the UI keeps handling messages.</summary>
        public static void WaitFor(Func<bool> condition)
        {
            System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (s, e) => { if (condition()) frame.Continue = false; };
            timer.Start();
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            timer.Stop();
        }
    }

    public class UpdateManager
    {
        private const string Owner = "koppepandayooo";
        private const string Repository = "SB";
        private const string releasesUrl = "https://github.com/" + Owner + "/" + Repository + "/releases";

        private class NewestRelease
        {
            public Version Version;
            public string InstallerUrl;
            public long InstallerSize;
        }

        public async Task AutoUpdateCheck(Version currentVersion)
        {
            try
            {
                NewestRelease newest = await GetNewestRelease();
                if (newest.Version > Normalize(currentVersion))
                {
                    ShowUpdateDialog(newest);
                }
            }
            catch { }
        }

        public async Task ManualUpdateCheck(Version currentVersion)
        {
            try
            {
                NewestRelease newest = await GetNewestRelease();
                if (newest.Version > Normalize(currentVersion))
                {
                    ShowUpdateDialog(newest);
                }
                else
                {
                    MessageBox.Show(UiLanguage.Get("Str_NoUpdate"));
                }
            }
            catch (Exception ex)
            {
                MessageBoxResult result = MessageBox.Show(
                        string.Format(UiLanguage.Get("Str_UpdateCheckFailedMessage"), ex.Message),
                        UiLanguage.Get("Str_UpdateCheckFailedTitle"),
                        MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(releasesUrl);
                }
            }
        }

        private void ShowUpdateDialog(NewestRelease newest)
        {
            // Called from a worker thread when the check runs at start-up
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                bool canInstall = newest.InstallerUrl != null;
                MessageBoxResult result = MessageBox.Show(
                            string.Format(UiLanguage.Get(canInstall ? "Str_UpdateInstallMessage" : "Str_UpdateAvailableMessage"), newest.Version, releasesUrl),
                            string.Format(UiLanguage.Get("Str_UpdateAvailableTitle"), newest.Version),
                            MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                if (canInstall) DownloadAndInstall(newest);
                else Process.Start(releasesUrl);
            });
        }

        private void DownloadAndInstall(NewestRelease newest)
        {
            string path = Path.Combine(Path.GetTempPath(), "SB_update", "SB_" + newest.Version + ".exe");
            Window progressWindow = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path)) File.Delete(path);

                ProgressBar bar = new ProgressBar { Height = 18, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 8, 0, 0) };
                TextBlock text = new TextBlock { Text = UiLanguage.Get("Str_UpdateDownloading") };
                StackPanel panel = new StackPanel { Margin = new Thickness(16) };
                panel.Children.Add(text);
                panel.Children.Add(bar);
                progressWindow = new Window
                {
                    Title = "SB",
                    Content = panel,
                    Width = 320,
                    SizeToContent = SizeToContent.Height,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };
                progressWindow.Show();

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "koppepandayooo-SB");
                    bool done = false;
                    Exception error = null;
                    client.DownloadProgressChanged += (s, e) => bar.Value = e.ProgressPercentage;
                    client.DownloadFileCompleted += (s, e) => { error = e.Error; done = true; };
                    client.DownloadFileAsync(new Uri(newest.InstallerUrl), path);

                    // Keep the window alive while waiting, without blocking the UI thread
                    DispatcherFrameHelper.WaitFor(() => done);
                    if (error != null) throw error;
                }
                progressWindow.Close();
                progressWindow = null;

                // Make sure it is the whole installer before running it
                if (new FileInfo(path).Length != newest.InstallerSize)
                    throw new IOException("The downloaded file is incomplete.");

                // The installer asks for administrator rights; if that is refused SB simply keeps running
                Process.Start(new ProcessStartInfo(path, "/S") { UseShellExecute = true, Verb = "runas" });
                System.Windows.Application.Current.Shutdown();
            }
            catch (Win32Exception)
            {
                // The administrator prompt was cancelled
                if (progressWindow != null) progressWindow.Close();
            }
            catch (Exception ex)
            {
                if (progressWindow != null) progressWindow.Close();
                MessageBoxResult result = MessageBox.Show(
                        string.Format(UiLanguage.Get("Str_UpdateCheckFailedMessage"), ex.Message),
                        UiLanguage.Get("Str_UpdateCheckFailedTitle"),
                        MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(releasesUrl);
                }
            }
        }

        private async Task<NewestRelease> GetNewestRelease()
        {
            try
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("koppepandayooo-SB"));
                Release latest = await client.Repository.Release.GetLatest(Owner, Repository);
                ReleaseAsset installer = latest.Assets.FirstOrDefault(a =>
                    Regex.IsMatch(a.Name, @"^SB_[\d.]+\.exe$") &&
                    a.BrowserDownloadUrl.StartsWith("https://github.com/" + Owner + "/" + Repository + "/releases/download/"));
                return new NewestRelease
                {
                    Version = ParseVersion(latest.TagName),
                    InstallerUrl = installer?.BrowserDownloadUrl,
                    InstallerSize = installer?.Size ?? 0
                };
            }
            catch (Exception ex)
            {
                throw new ApiException("Unable to get newest version from GitHub.", ex);
            }
        }

        /// <summary>Reads "v1.2.3" / "1.2.3.4" style tags; missing parts are treated as 0.</summary>
        private static Version ParseVersion(string tag)
        {
            Match match = Regex.Match(tag ?? "", @"\d+(\.\d+){0,3}");
            if (!match.Success)
                throw new FormatException("Unrecognized release tag: " + tag);
            return Normalize(Version.Parse(match.Value.Contains(".") ? match.Value : match.Value + ".0"));
        }

        // 1.0.0 would compare as older than 1.0.0.0, so always use four parts
        private static Version Normalize(Version version)
        {
            return new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
        }
    }
}
