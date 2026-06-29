using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Catalog.Entities;
using NetflixSkipIntroMensageria.Infrastructure.Data.Entities;

namespace NetflixSkipIntroMensageria.Infrastructure.Data;

public class NetflixDbContext : DbContext
{
    public NetflixDbContext(DbContextOptions<NetflixDbContext> options) : base(options) { }

    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<PlaybackStateEntity> PlaybackStates => Set<PlaybackStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Episodes ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Episode>(e =>
        {
            e.ToTable("Episodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever(); // Id vem do domínio, não auto-increment
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.VideoStorageKey).IsRequired().HasMaxLength(500);
        });

        // ── PlaybackStates ────────────────────────────────────────────────────
        modelBuilder.Entity<PlaybackStateEntity>(e =>
        {
            e.ToTable("PlaybackStates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.VideoStorageKey).IsRequired().HasMaxLength(500);

            // Índice único (UserId, EpisodeId): garante idempotência — o mesmo evento
            // processado duas vezes não gera dois registros, só sobrescreve.
            e.HasIndex(x => new { x.UserId, x.EpisodeId }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
