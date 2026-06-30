namespace NetflixSkipIntroMensageria.SharedKernel.Events;

/// <summary>
/// Emitido pelo player assim que inicia a reprodução de um episódio.
/// O Consumer usa este evento para marcar o PlaybackState como consumido (IsConsumed = true),
/// evitando que o mesmo "skip" seja aplicado numa segunda abertura do episódio.
/// </summary>
public record PlaybackStartedEvent(
    Guid UserId,
    int EpisodeId,
    int StartAtSeconds,
    Guid SessionId,
    DateTime StartedAt
);
