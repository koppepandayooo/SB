using NAudio.Vorbis;

namespace SB.SoundPlayer
{
    public class OggFileReader : VorbisWaveReader, IAudioReader
    {
        public OggFileReader(string fileName) : base(fileName)
        { }
    }
}
