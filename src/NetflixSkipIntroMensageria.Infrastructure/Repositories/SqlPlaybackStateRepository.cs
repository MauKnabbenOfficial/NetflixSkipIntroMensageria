using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Infrastructure.Data;
using NetflixSkipIntroMensageria.Infrastructure.Data.Entities;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Repositories;

/// <summary>
/// Implementação SQL Server do repositório de estado de reprodução.
///
/// Garante três propriedades de integridade no GetAsync:
///   1. SessionId match  — rejeita estados de sessões anteriores
///   2. TTL              — rejeita estados expirados (ExpiresAt < agora)
///   3. IsConsumed       — rejeita estados já aplicados pelo player
///
/// O SaveAsync usa upsert: mesmo evento processado duas vezes → mesmo resultado.
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
                UserId          = state.UserId,
                EpisodeId       = state.EpisodeId,
                StartAtSeconds  = state.StartAtSeconds,
                VideoStorageKey = state.VideoStorageKey,
                SessionId       = state.SessionId,
                ExpiresAt       = state.ExpiresAt,
                IsConsumed      = false,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            });
        }
        else
        {
            // Nova sessão sobrescreve estado anterior (mesmo que já consumido/expirado)
            existing.StartAtSeconds  = state.StartAtSeconds;
            existing.VideoStorageKey = state.VideoStorageKey;
            existing.SessionId       = state.SessionId;
            existing.ExpiresAt       = state.ExpiresAt;
            existing.IsConsumed      = false;   // Reset: nova sessão, novo estado
            existing.UpdatedAt       = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PlaybackState?> GetAsync(Guid userId, int episodeId, Guid sessionId)
    {
        var entity = await _db.PlaybackStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId    == userId    &&
                x.EpisodeId == episodeId &&
                x.SessionId == sessionId &&          // Mesma sessão
                x.ExpiresAt >  DateTime.UtcNow &&   // Não expirou
                !x.IsConsumed);                      // Não consumido ainda

        if (entity is null) return null;

        return new PlaybackState(
            entity.UserId,
            entity.EpisodeId,
            entity.StartAtSeconds,
            entity.VideoStorageKey,
            entity.SessionId,
            entity.ExpiresAt,
            entity.IsConsumed);
    }

    public async Task MarkConsumedAsync(Guid userId, int episodeId)
    {
        var entity = await _db.PlaybackStates
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EpisodeId == episodeId);

        if (entity is null) return;

        entity.IsConsumed = true;
        entity.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
