using NetflixSkipIntroMensageria.Application.DTOs;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Application.Services;

public class EpisodeService : IEpisodeService
{
    private readonly IPlaybackStateRepository _playbackState;
    private readonly ICatalogRepository _catalog;

    public EpisodeService(IPlaybackStateRepository playbackState, ICatalogRepository catalog)
    {
        _playbackState = playbackState;
        _catalog = catalog;
    }

    public async Task<PlaybackStateDto?> GetPlaybackStateAsync(Guid userId, int episodeId)
    {
        // 1. Estado pré-computado pelo Consumer ao processar o evento Kafka
        var state = await _playbackState.GetAsync(userId, episodeId);
        if (state is not null)
            return new PlaybackStateDto(
                episodeId,
                state.StartAtSeconds,
                state.VideoStorageKey,
                Source: "pre-computed");

        // 2. Fallback: usuário abriu o episódio direto, sem vir do anterior
        var episode = _catalog.GetById(episodeId);
        if (episode is null) return null;

        return new PlaybackStateDto(
            episodeId,
            StartAtSeconds: 0,
            episode.VideoStorageKey,
            Source: "catalog-default");
    }
}
