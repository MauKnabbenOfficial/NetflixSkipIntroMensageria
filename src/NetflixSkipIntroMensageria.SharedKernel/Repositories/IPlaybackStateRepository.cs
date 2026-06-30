using NetflixSkipIntroMensageria.SharedKernel.Models;

namespace NetflixSkipIntroMensageria.SharedKernel.Repositories;

/// <summary>
/// Interface definida no SharedKernel para que tanto Application quanto Infrastructure
/// possam referenciá-la sem criar dependência circular.
///
/// Regra: interfaces de repositório pertencem à camada de domínio/aplicação.
///        implementações (SQL, InMemory) pertencem à Infrastructure.
/// </summary>
public interface IPlaybackStateRepository
{
    Task SaveAsync(PlaybackState state);

    /// <summary>
    /// Retorna o estado pré-computado somente se:
    ///   - SessionId corresponde à sessão atual (não é stale de sessão anterior)
    ///   - Não expirou (ExpiresAt > agora)
    ///   - Não foi consumido ainda (IsConsumed = false)
    /// </summary>
    Task<PlaybackState?> GetAsync(Guid userId, int episodeId, Guid sessionId);

    /// <summary>
    /// Marca o estado como consumido após o player iniciar a reprodução.
    /// Impede que o mesmo "skip" seja aplicado numa segunda abertura do episódio.
    /// </summary>
    Task MarkConsumedAsync(Guid userId, int episodeId);
}
