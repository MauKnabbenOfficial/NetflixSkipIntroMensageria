using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Catalog.Entities;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.Infrastructure.Data;

namespace NetflixSkipIntroMensageria.Infrastructure.Repositories;

/// <summary>
/// Implementação SQL Server do catálogo de episódios.
/// Leitura pura: catálogo é imutável em runtime (não há escrita aqui).
/// AsNoTracking() em todas as queries — sem necessidade de rastreamento de mudanças.
/// </summary>
public class SqlCatalogRepository : ICatalogRepository
{
    private readonly NetflixDbContext _db;

    public SqlCatalogRepository(NetflixDbContext db)
    {
        _db = db;
    }

    public Episode? GetById(int episodeId) =>
        _db.Episodes
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == episodeId);

    public IReadOnlyList<Episode> GetAll() =>
        _db.Episodes
            .AsNoTracking()
            .OrderBy(e => e.Season)
            .ThenBy(e => e.Number)
            .ToList()
            .AsReadOnly();
}
