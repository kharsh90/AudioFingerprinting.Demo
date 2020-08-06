using SoundFingerprinting.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoundFingerprinting.AddictedCS.Demo.Repositories
{
    public interface IAudioFingerprintRepository
    {
        void SaveAudioFingerprints(Hashes hashedFingerprints, string trackInfo);
        Dictionary<string, List<Data.HashedFingerprint>> GetAudioFingerprintHashes();
    }
}
