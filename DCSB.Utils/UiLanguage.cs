using System;
using System.Globalization;
using System.Windows;

namespace DCSB.Utils
{
    /// <summary>
    /// UI language handling. Strings live in resource dictionaries (Strings.en.xaml, Strings.ja.xaml)
    /// that are swapped at runtime; XAML uses DynamicResource, code uses <see cref="Get"/>.
    /// </summary>
    public static class UiLanguage
    {
        public const string Auto = "auto";

        public static string Current { get; private set; } = "en";

        public static event Action LanguageChanged;

        /// <summary>Maps a stored setting (auto / en / ja) to a concrete language.</summary>
        public static string Resolve(string setting)
        {
            if (setting == "en" || setting == "ja")
                return setting;
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ? "ja" : "en";
        }

        public static void Apply(string setting)
        {
            string language = Resolve(setting);
            Application app = Application.Current;
            if (app == null) return;

            var dictionaries = app.Resources.MergedDictionaries;
            for (int i = dictionaries.Count - 1; i >= 0; i--)
            {
                Uri source = dictionaries[i].Source;
                if (source != null && source.OriginalString.Contains("Strings."))
                    dictionaries.RemoveAt(i);
            }
            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"/DCSB.Icons;component/Strings.{language}.xaml", UriKind.Relative)
            });

            Current = language;
            LanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            return Application.Current?.TryFindResource(key) as string ?? key;
        }
    }
}
