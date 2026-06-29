using NetflixSkipIntroMensageria.Catalog.Entities;

namespace NetflixSkipIntroMensageria.Catalog.Repositories;

/// <summary>
/// Repositório in-memory com seed de 5 episódios.
///
/// Em produção isto seria um acesso ao banco de dados (SQL Server, PostgreSQL etc.)
/// com a tabela Episodes contendo as colunas VideoStorageKey, IntroStartSeconds, IntroEndSeconds.
///
/// Todos os episódios aqui apontam para "videos/GTAVI.mp4" — um único arquivo físico —
/// simulando o comportamento real sem precisar de múltiplos vídeos.
/// </summary>
public class InMemoryCatalogRepository : ICatalogRepository
{
    private static readonly Dictionary<int, Episode> _episodes = new()
    {
        [1] = new Episode
        {
            Id = 1, Title = "Piloto", Season = 1, Number = 1,
            IntroStartSeconds = 10, IntroEndSeconds = 55,
            VideoStorageKey = "videos/GTAVI.mp4"
        },
        [2] = new Episode
        {
            Id = 2, Title = "O Começo", Season = 1, Number = 2,
            IntroStartSeconds = 12, IntroEndSeconds = 58,
            VideoStorageKey = "videos/GTAVI.mp4"
        },
        [3] = new Episode
        {
            Id = 3, Title = "A Virada", Season = 1, Number = 3,
            IntroStartSeconds = 8, IntroEndSeconds = 52,
            VideoStorageKey = "videos/GTAVI.mp4"
        },
        [4] = new Episode
        {
            Id = 4, Title = "O Confronto", Season = 1, Number = 4,
            IntroStartSeconds = 15, IntroEndSeconds = 60,
            VideoStorageKey = "videos/GTAVI.mp4"
        },
        [5] = new Episode
        {
            Id = 5, Title = "Finale", Season = 1, Number = 5,
            IntroStartSeconds = 0, IntroEndSeconds = 0, // último ep, sem próximo
            VideoStorageKey = "videos/GTAVI.mp4"
        },
    };

    public Episode? GetById(int episodeId) =>
        _episodes.TryGetValue(episodeId, out var ep) ? ep : null;

    public IReadOnlyList<Episode> GetAll() =>
        _episodes.Values.ToList().AsReadOnly();
}
