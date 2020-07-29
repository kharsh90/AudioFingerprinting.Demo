using SoundFingerprinting.AddictedCS.Demo.EFDatabase;
using SoundFingerprinting.Data;
using System.Collections.Generic;
using System.Linq;

namespace SoundFingerprinting.AddictedCS.Demo.Repositories
{
    public class AudioFingerprintRepository : IAudioFingerprintRepository
    {
        public void SaveAudioFingerprints(Hashes hashedFingerprints,string trackInfo)
        {
            using (SoundfingerprintingDbContext context = new SoundfingerprintingDbContext())
            {
                if (!context.HashedFingerprint.Any(_ => _.TrackInfo == trackInfo))
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
                            StreamId = hashedFingerprints.StreamId,
                            TrackInfo = trackInfo
                        };
                        hashedFingerprintRows.Add(hfRow);
                    }
                    context.HashedFingerprint.AddRange(hashedFingerprintRows);
                    context.SaveChanges();
                }
            }
        }

        Dictionary<string, List<Data.HashedFingerprint>> IAudioFingerprintRepository.GetAudioFingerprintHashes()
        {
            var hashedFingerprints = new Dictionary<string,List<Data.HashedFingerprint>>();
            using (var context = new SoundfingerprintingDbContext())
            {
                var audioFingerprints = context.HashedFingerprint;
                var key = string.Empty;
                foreach (var audioFingerprint in audioFingerprints)
                {
                    var fingerprint = new Data.HashedFingerprint(
                        audioFingerprint.Hashbins,
                        audioFingerprint.SequenceNumber,
                        (float)audioFingerprint.StartsAt,
                        audioFingerprint.OriginalPoint
                    );

                    if (key != audioFingerprint.TrackInfo)
                    {
                        key = audioFingerprint.TrackInfo;
                        hashedFingerprints.Add(key, new List<Data.HashedFingerprint> { fingerprint});
                    }
                    else
                    {
                        hashedFingerprints[key].Add(fingerprint);
                    } 
                }
            }
            return hashedFingerprints;
        }
    }
}
