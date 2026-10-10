using NAudio.Wave;

namespace SB.SoundPlayer
{
    internal class FileReader : AudioFileReader, IAudioReader
    {
        public FileReader(string fileName) : base(fileName)
        { }
    }
}
