namespace NetflixSkipIntroMensageria.Infrastructure.Data.Entities;

/// <summary>
/// Registra a última posição conhecida de um usuário num episódio.
/// Alimentado pelo consumer de PlaybackPositionEvent (heartbeat a cada 30s).
/// Usado para a feature "continue watching" — próxima sessão começa de onde parou.
/// </summary>
public class WatchProgressEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int EpisodeId { get; set; }
    public int LastPositionSeconds { get; set; }
    public Guid SessionId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
