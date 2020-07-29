using System.ComponentModel.DataAnnotations.Schema;

namespace SoundFingerprinting.AddictedCS.Demo.EFDatabase
{
    [Table("HashedFingerprint")]
    public class HashedFingerprint
    {
        public long Id { get; set; }
        public uint SequenceNumber { get; set; }
        public double StartsAt { get; set; }
        public byte[] OriginalPoint { get; set; }
        public int[] Hashbins { get; set; }
        public string StreamId { get; set; }

        public string TrackInfo { get; set; }
    }
}
