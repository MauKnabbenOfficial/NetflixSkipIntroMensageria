using NetflixSkipIntroMensageria.Application.DTOs;

namespace NetflixSkipIntroMensageria.Application.Services;

public interface IEpisodeService
{
    /// <summary>
    /// Retorna o estado de reprodução valido para a sessao atual.
    /// Aplica as tres verificacoes de integridade: sessionId, TTL e isConsumed.
    /// </summary>
    Task<PlaybackStateDto?> GetPlaybackStateAsync(Guid userId, int episodeId, Guid sessionId);
}
