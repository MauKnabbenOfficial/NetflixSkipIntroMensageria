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

    // ── Campos de integridade ─────────────────────────────────────────────
    public Guid SessionId { get; set; }           // Âncora de sessão — ignora estados stale
    public DateTime ExpiresAt { get; set; }        // TTL: CreatedAt + 2h
    public bool IsConsumed { get; set; } = false;  // True após o player confirmar reprodução

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
