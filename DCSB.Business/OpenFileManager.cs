using DCSB.Utils;
using Microsoft.Win32;

namespace DCSB.Business
{
    public class OpenFileManager
    {
        public static readonly string[] SoundExtensions = { ".wma", ".mp3", ".wav", ".ogg", ".m4a", ".aiff", ".flac" };

        public string[] OpenSoundFiles()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = UiLanguage.Get("Str_ChooseSoundFiles"),
                Filter = UiLanguage.Get("Str_SoundFilesFilter") + " (*.wma; *.mp3; *.wav; *.ogg; *.m4a; *.aiff; *.flac)|*.wma;*.mp3;*.wav;*.ogg;*.m4a;*.aiff;*.flac",
                AddExtension = true,
                RestoreDirectory = true,
                Multiselect = true
            };

            bool? result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                return fileDialog.FileNames;
            }
            return null;
        }
    }
}
