using System.Collections.Concurrent;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Repositories;

/// <summary>
/// Repositório in-memory thread-safe para estado de reprodução.
/// Implementa a mesma interface async que o SqlPlaybackStateRepository —
/// o caller não sabe (nem precisa saber) se está falando com SQL ou memória.
/// Aplica as mesmas regras de integridade: sessionId, TTL e isConsumed.
/// </summary>
public class InMemoryPlaybackStateRepository : IPlaybackStateRepository
{
    private readonly ConcurrentDictionary<(Guid, int), PlaybackState> _store = new();

    public Task SaveAsync(PlaybackState state)
    {
        _store[(state.UserId, state.EpisodeId)] = state with { IsConsumed = false };
        return Task.CompletedTask;
    }

    public Task<PlaybackState?> GetAsync(Guid userId, int episodeId, Guid sessionId)
    {
        _store.TryGetValue((userId, episodeId), out var state);

        if (state is null) return Task.FromResult<PlaybackState?>(null);

        // Mesmas três verificações do repositório SQL
        if (state.SessionId != sessionId)    return Task.FromResult<PlaybackState?>(null);
        if (state.ExpiresAt < DateTime.UtcNow) return Task.FromResult<PlaybackState?>(null);
        if (state.IsConsumed)                return Task.FromResult<PlaybackState?>(null);

        return Task.FromResult<PlaybackState?>(state);
    }

    public Task MarkConsumedAsync(Guid userId, int episodeId)
    {
        if (_store.TryGetValue((userId, episodeId), out var state))
            _store[(userId, episodeId)] = state with { IsConsumed = true };

        return Task.CompletedTask;
    }
}
