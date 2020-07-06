using SoundFingerprinting.Data;

namespace SoundFingerprinting.AddictedCS.Demo.Repositories
{
    public interface IAudioFingerprintRepository
    {
        void SaveAudioFingerprints(Hashes hashedFingerprints);
        Hashes GetAudioFingerprintHashes();
    }
}
