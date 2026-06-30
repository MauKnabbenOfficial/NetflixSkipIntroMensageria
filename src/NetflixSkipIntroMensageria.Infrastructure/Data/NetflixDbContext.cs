using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Catalog.Entities;
using NetflixSkipIntroMensageria.Infrastructure.Data.Entities;

namespace NetflixSkipIntroMensageria.Infrastructure.Data;

public class NetflixDbContext : DbContext
{
    public NetflixDbContext(DbContextOptions<NetflixDbContext> options) : base(options) { }

    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<PlaybackStateEntity> PlaybackStates => Set<PlaybackStateEntity>();
    public DbSet<WatchProgressEntity> WatchProgress => Set<WatchProgressEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Episodes ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Episode>(e =>
        {
            e.ToTable("Episodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
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

            // Índice único (UserId, EpisodeId): upsert sobrescreve o estado anterior.
            // Quando uma nova sessão gera um estado para o mesmo episódio, o antigo é substituído.
            e.HasIndex(x => new { x.UserId, x.EpisodeId }).IsUnique();
        });

        // ── WatchProgress ─────────────────────────────────────────────────────
        modelBuilder.Entity<WatchProgressEntity>(e =>
        {
            e.ToTable("WatchProgress");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();

            // Índice único (UserId, EpisodeId): upsert mantém apenas o progresso mais recente.
            e.HasIndex(x => new { x.UserId, x.EpisodeId }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
