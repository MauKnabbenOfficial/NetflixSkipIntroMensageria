namespace NetflixSkipIntroMensageria.Infrastructure.Data.Entities;

/// <summary>
/// Entidade EF Core para a tabela PlaybackStates.
/// Separada do record de domínio (SharedKernel.Models.PlaybackState) intencionalmente:
/// entidades de banco carregam conceitos de persistência (Id auto-increment, timestamps)
/// que não pertencem ao domínio.
/// </summary>
public class PlaybackStateEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int EpisodeId { get; set; }
    public int StartAtSeconds { get; set; }
    public required string VideoStorageKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
