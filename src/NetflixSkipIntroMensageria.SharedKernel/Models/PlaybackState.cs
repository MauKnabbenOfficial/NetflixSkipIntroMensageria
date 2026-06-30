namespace NetflixSkipIntroMensageria.SharedKernel.Models;

/// <summary>
/// Estado de reprodução pré-computado para um usuário em um episódio específico.
/// Gerado pelo Consumer quando processa um EpisodeCompletedEvent:
///   usuário terminou ep N → calculamos onde ep N+1 deve começar (após intro).
///
/// Integridade garantida por três campos:
///   SessionId  — ignora estados de sessões anteriores (vídeo diferente, dia diferente)
///   ExpiresAt  — TTL de 2h: estado stale nunca é aplicado
///   IsConsumed — uma vez lido e aplicado pelo player, não é reutilizado
/// </summary>
public record PlaybackState(
    Guid UserId,
    int EpisodeId,
    int StartAtSeconds,
    string VideoStorageKey,
    Guid SessionId,
    DateTime ExpiresAt,
    bool IsConsumed = false
);
