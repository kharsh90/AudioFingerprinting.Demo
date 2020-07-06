using Microsoft.EntityFrameworkCore;

namespace SoundFingerprinting.AddictedCS.Demo.EFDatabase
{
    public class SoundfingerprintingDbContext :DbContext
    {
        private const string DB_CONNECTION_STRING = "Host=localhost;Database=postgres;Username=postgres;Password=postgres";
        public DbSet<HashedFingerprint> HashedFingerprint { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(DB_CONNECTION_STRING);
    }
}