using Microsoft.AspNetCore.Mvc;
using NetflixSkipIntroMensageria.Application.Services;

namespace NetflixSkipIntroMensageria.API.Controllers;

[ApiController]
[Route("catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Lista todos os episódios do catálogo.
    /// Útil para inspeção durante testes e para o front montar a grade de episódios.
    /// </summary>
    [HttpGet("episodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_catalogService.GetAllEpisodes());

    /// <summary>
    /// Retorna um episódio específico do catálogo.
    /// </summary>
    [HttpGet("episodes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        var episode = _catalogService.GetEpisodeById(id);
        return episode is null ? NotFound() : Ok(episode);
    }
}
