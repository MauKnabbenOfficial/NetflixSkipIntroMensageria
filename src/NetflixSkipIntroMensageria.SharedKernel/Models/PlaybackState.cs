namespace NetflixSkipIntroMensageria.SharedKernel.Models;

/// <summary>
/// Estado de reprodução pré-computado para um usuário em um episódio específico.
/// Gerado pelo Consumer quando processa um EpisodeCompletedEvent:
///   usuário terminou ep N → calculamos onde ep N+1 deve começar (após intro).
/// </summary>
public record PlaybackState(
    Guid UserId,
    int EpisodeId,
    int StartAtSeconds,
    string VideoStorageKey
);
