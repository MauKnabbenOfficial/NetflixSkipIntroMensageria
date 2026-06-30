namespace NetflixSkipIntroMensageria.Application.DTOs;

/// <summary>
/// Resposta do endpoint GET /episodes/{id}/playback-state.
/// O player usa este DTO para saber onde iniciar a reproducao.
/// SessionId e devolvido para que o frontend confirme a reproducao via POST /playback-started.
/// </summary>
public record PlaybackStateDto(
    int EpisodeId,
    int StartAtSeconds,
    string VideoStorageKey,
    Guid SessionId,
    string Source    // "pre-computed" (veio do Kafka) ou "catalog-default" (acesso direto)
);
