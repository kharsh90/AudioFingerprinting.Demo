using SoundFingerprinting.AddictedCS.Demo.EFDatabase;
using SoundFingerprinting.Data;
using System.Collections.Generic;
using System.Linq;

namespace SoundFingerprinting.AddictedCS.Demo.Repositories
{
    public class AudioFingerprintRepository : IAudioFingerprintRepository
    {
        public void SaveAudioFingerprints(Hashes hashedFingerprints)
        {
            using (SoundfingerprintingDbContext context = new SoundfingerprintingDbContext())
            {
                if (!context.HashedFingerprint.Any(_ => _.StreamId == hashedFingerprints.StreamId))
                {

                    var hashedFingerprintRows = new List<EFDatabase.HashedFingerprint>();

                    foreach (var fingerprint in hashedFingerprints)
                    {
                        var hfRow = new EFDatabase.HashedFingerprint
                        {
                            OriginalPoint = fingerprint.OriginalPoint,
                            StartsAt = fingerprint.StartsAt,
                            SequenceNumber = fingerprint.SequenceNumber,
                            Hashbins = fingerprint.HashBins,
                            StreamId = hashedFingerprints.StreamId
                        };
                        hashedFingerprintRows.Add(hfRow);
                    }
                    context.HashedFingerprint.AddRange(hashedFingerprintRows);
                    context.SaveChanges();
                }
            }
        }

        Hashes IAudioFingerprintRepository.GetAudioFingerprintHashes()
        {
            var hashedFingerprints = new List<Data.HashedFingerprint>();
            using (var context = new SoundfingerprintingDbContext())
            {
                var audioFingerprints = context.HashedFingerprint;

                foreach (var audioFingerprint in audioFingerprints)
                {
                    var fingerprint = new Data.HashedFingerprint(
                        audioFingerprint.Hashbins,
                        audioFingerprint.SequenceNumber,
                        (float)audioFingerprint.StartsAt,
                        audioFingerprint.OriginalPoint
                        );
                    hashedFingerprints.Add(fingerprint);
                }
            }
            var hashes = new Hashes(hashedFingerprints, 1);
            return hashes;
        }
    }
}
