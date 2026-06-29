using NetflixSkipIntroMensageria.Application.DTOs;

namespace NetflixSkipIntroMensageria.Application.Services;

public interface ICatalogService
{
    IReadOnlyList<EpisodeDto> GetAllEpisodes();
    EpisodeDto? GetEpisodeById(int episodeId);
}
