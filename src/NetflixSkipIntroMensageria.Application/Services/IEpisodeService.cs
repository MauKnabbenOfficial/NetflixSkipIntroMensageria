using NetflixSkipIntroMensageria.Application.DTOs;

namespace NetflixSkipIntroMensageria.Application.Services;

public interface IEpisodeService
{
    /// <summary>
    /// Retorna onde o player deve iniciar a reprodução do episódio.
    /// Consulta o estado pré-computado pelo Consumer (via Kafka) primeiro;
    /// se não existir, retorna o default do catálogo (início do vídeo).
    /// </summary>
    Task<PlaybackStateDto?> GetPlaybackStateAsync(Guid userId, int episodeId);
}
