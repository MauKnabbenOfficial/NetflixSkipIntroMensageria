using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Infrastructure.Data;
using NetflixSkipIntroMensageria.Infrastructure.Data.Entities;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Repositories;

/// <summary>
/// Implementação SQL Server do repositório de estado de reprodução.
/// Usa upsert (AddOrUpdate) para garantir idempotência:
/// se o Consumer processar o mesmo evento duas vezes, o resultado é o mesmo.
/// </summary>
public class SqlPlaybackStateRepository : IPlaybackStateRepository
{
    private readonly NetflixDbContext _db;

    public SqlPlaybackStateRepository(NetflixDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(PlaybackState state)
    {
        var existing = await _db.PlaybackStates
            .FirstOrDefaultAsync(x => x.UserId == state.UserId && x.EpisodeId == state.EpisodeId);

        if (existing is null)
        {
            _db.PlaybackStates.Add(new PlaybackStateEntity
            {
                UserId = state.UserId,
                EpisodeId = state.EpisodeId,
                StartAtSeconds = state.StartAtSeconds,
                VideoStorageKey = state.VideoStorageKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            // Atualiza — o Consumer pode reprocessar um evento (ex: após DLQ retry)
            existing.StartAtSeconds = state.StartAtSeconds;
            existing.VideoStorageKey = state.VideoStorageKey;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PlaybackState?> GetAsync(Guid userId, int episodeId)
    {
        var entity = await _db.PlaybackStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EpisodeId == episodeId);

        if (entity is null) return null;

        return new PlaybackState(entity.UserId, entity.EpisodeId, entity.StartAtSeconds, entity.VideoStorageKey);
    }
}
