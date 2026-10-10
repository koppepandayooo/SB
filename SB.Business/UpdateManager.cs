using Octokit;
using SB.Utils;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SB.Business
{
    /// <summary>
    /// Tells the user when a newer release is published on GitHub. It never downloads or installs anything:
    /// the user is only offered to open the releases page.
    /// </summary>
    public class UpdateManager
    {
        private const string Owner = "koppepandayooo";
        private const string Repository = "SB";
        private const string releasesUrl = "https://github.com/" + Owner + "/" + Repository + "/releases";

        public async Task AutoUpdateCheck(Version currentVersion)
        {
            try
            {
                Version newVersion = await GetNewestVersion();
                if (newVersion > Normalize(currentVersion))
                {
                    ShowUpdateDialog(newVersion);
                }
            }
            catch { }
        }

        public async Task ManualUpdateCheck(Version currentVersion)
        {
            try
            {
                Version newVersion = await GetNewestVersion();
                if (newVersion > Normalize(currentVersion))
                {
                    ShowUpdateDialog(newVersion);
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

        private void ShowUpdateDialog(Version newVersion)
        {
            MessageBoxResult result = MessageBox.Show(
                        string.Format(UiLanguage.Get("Str_UpdateAvailableMessage"), newVersion, releasesUrl),
                        string.Format(UiLanguage.Get("Str_UpdateAvailableTitle"), newVersion),
                        MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(releasesUrl);
            }
        }

        private async Task<Version> GetNewestVersion()
        {
            try
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("koppepandayooo-SB"));
                Release latest = await client.Repository.Release.GetLatest(Owner, Repository);
                return ParseVersion(latest.TagName);
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
