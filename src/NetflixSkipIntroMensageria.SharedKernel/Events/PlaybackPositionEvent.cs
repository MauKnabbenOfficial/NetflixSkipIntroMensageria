namespace NetflixSkipIntroMensageria.SharedKernel.Events;

/// <summary>
/// Heartbeat de progresso emitido a cada 30 segundos enquanto o usuário assiste.
/// Permite "continue watching": na próxima sessão o sistema sabe exatamente onde parar.
/// Também detecta abandono — se o position parar de avançar, a sessão está inativa.
/// </summary>
public record PlaybackPositionEvent(
    Guid UserId,
    int EpisodeId,
    int PositionSeconds,
    Guid SessionId,
    DateTime OccurredAt
);
