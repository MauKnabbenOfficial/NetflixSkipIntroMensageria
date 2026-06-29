namespace NetflixSkipIntroMensageria.Application.DTOs;

/// <summary>
/// Resposta do endpoint GET /episodes/{id}/playback-state.
/// O player usa este DTO para saber onde iniciar a reprodução.
/// </summary>
public record PlaybackStateDto(
    int EpisodeId,
    int StartAtSeconds,
    string VideoStorageKey,
    string Source              // "pre-computed" (veio do Kafka) ou "catalog-default" (acesso direto)
);
