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
    Task<PlaybackState?> GetAsync(Guid userId, int episodeId);
}
