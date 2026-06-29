using System.Collections.Concurrent;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Repositories;

/// <summary>
/// Repositório in-memory thread-safe para estado de reprodução.
/// Implementa a mesma interface async que o SqlPlaybackStateRepository —
/// o caller não sabe (nem precisa saber) se está falando com SQL ou memória.
/// </summary>
public class InMemoryPlaybackStateRepository : IPlaybackStateRepository
{
    private readonly ConcurrentDictionary<(Guid, int), PlaybackState> _store = new();

    public Task SaveAsync(PlaybackState state)
    {
        _store[(state.UserId, state.EpisodeId)] = state;
        return Task.CompletedTask;
    }

    public Task<PlaybackState?> GetAsync(Guid userId, int episodeId)
    {
        _store.TryGetValue((userId, episodeId), out var state);
        return Task.FromResult(state);
    }
}
