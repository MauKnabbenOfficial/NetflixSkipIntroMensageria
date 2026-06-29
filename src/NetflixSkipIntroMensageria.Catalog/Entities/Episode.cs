namespace NetflixSkipIntroMensageria.Catalog.Entities;

/// <summary>
/// Representa um episódio no catálogo.
///
/// Em produção: VideoStorageKey é uma chave no S3/Azure Blob (ex: "series/s01/e06.mp4").
/// A API gera uma pre-signed URL com TTL para o player acessar o CDN diretamente.
///
/// Na simulação: todos os episódios apontam para o mesmo arquivo físico,
/// mas cada um tem tempos de intro diferentes — exatamente como seria em produção.
/// </summary>
public class Episode
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public int Season { get; init; }
    public int Number { get; init; }
    public int IntroStartSeconds { get; init; }
    public int IntroEndSeconds { get; init; }

    /// <summary>
    /// Caminho relativo no storage (S3 key, Azure Blob path, etc.).
    /// Todos apontam para o mesmo arquivo nesta simulação.
    /// </summary>
    public required string VideoStorageKey { get; init; }
}
