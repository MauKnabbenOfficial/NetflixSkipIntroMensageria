using NetflixSkipIntroMensageria.Application.DTOs;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Application.Services;

/// <summary>
/// Application service responsavel por consultar o estado de reproducao.
/// Depende apenas de interfaces definidas no SharedKernel e Catalog —
/// sem dependencia de Infrastructure ou Streaming (producao Kafka fica no Controller).
/// </summary>
public class EpisodeService : IEpisodeService
{
    private readonly IPlaybackStateRepository _playbackState;
    private readonly ICatalogRepository _catalog;

    public EpisodeService(IPlaybackStateRepository playbackState, ICatalogRepository catalog)
    {
        _playbackState = playbackState;
        _catalog       = catalog;
    }

    public async Task<PlaybackStateDto?> GetPlaybackStateAsync(Guid userId, int episodeId, Guid sessionId)
    {
        // 1. Estado pre-computado pelo Consumer — valido somente se mesma sessao, nao expirado, nao consumido
        var state = await _playbackState.GetAsync(userId, episodeId, sessionId);
        if (state is not null)
            return new PlaybackStateDto(
                episodeId,
                state.StartAtSeconds,
                state.VideoStorageKey,
                SessionId: sessionId,
                Source: "pre-computed");

        // 2. Fallback: usuario abriu o episodio direto, sem vir do anterior
        var episode = _catalog.GetById(episodeId);
        if (episode is null) return null;

        return new PlaybackStateDto(
            episodeId,
            StartAtSeconds: 0,
            episode.VideoStorageKey,
            SessionId: sessionId,
            Source: "catalog-default");
    }
}
