using NetflixSkipIntroMensageria.Catalog.Entities;

namespace NetflixSkipIntroMensageria.Catalog.Repositories;

public interface ICatalogRepository
{
    Episode? GetById(int episodeId);
    IReadOnlyList<Episode> GetAll();
}
