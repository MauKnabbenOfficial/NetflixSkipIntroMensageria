using NetflixSkipIntroMensageria.Application.DTOs;
using NetflixSkipIntroMensageria.Catalog.Repositories;

namespace NetflixSkipIntroMensageria.Application.Services;

/// <summary>
/// Orquestra acesso ao catálogo e mapeia entidades de domínio para DTOs.
/// Controllers nunca tocam nos repositórios diretamente — passam sempre pelo Service.
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _repository;

    public CatalogService(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<EpisodeDto> GetAllEpisodes()
    {
        var eps = _repository.GetAll()
            .Select(e => new EpisodeDto(
                e.Id, e.Title, e.Season, e.Number,
                e.IntroStartSeconds, e.IntroEndSeconds, e.VideoStorageKey))
            .ToList()
            .AsReadOnly();

        return eps;
    }

    public EpisodeDto? GetEpisodeById(int episodeId)
    {
        var episode = _repository.GetById(episodeId);
        if (episode is null) return null;

        return new EpisodeDto(
            episode.Id, episode.Title, episode.Season, episode.Number,
            episode.IntroStartSeconds, episode.IntroEndSeconds, episode.VideoStorageKey);
    }
}
