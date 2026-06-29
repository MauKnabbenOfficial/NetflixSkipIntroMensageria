using Microsoft.AspNetCore.Mvc;
using NetflixSkipIntroMensageria.Application.Services;
using NetflixSkipIntroMensageria.SharedKernel.Events;
using NetflixSkipIntroMensageria.Streaming.Messaging;

namespace NetflixSkipIntroMensageria.API.Controllers;

[ApiController]
[Route("episodes")]
public class EpisodesController : ControllerBase
{
    private readonly EpisodeProducer _producer;
    private readonly IEpisodeService _episodeService;

    public EpisodesController(EpisodeProducer producer, IEpisodeService episodeService)
    {
        _producer = producer;
        _episodeService = episodeService;
    }

    /// <summary>
    /// Sinaliza que o usuário terminou o episódio e clicou em "próximo".
    /// Publica um EpisodeCompletedEvent no Kafka.
    /// O Consumer processa de forma assíncrona e persiste o PlaybackState.
    /// </summary>
    [HttpPost("{episodeId}/completed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> CompleteEpisode(int episodeId)
    {
        var evento = new EpisodeCompletedEvent(
            UserId: Guid.NewGuid(),       // TODO: extrair do JWT após implementar auth
            EpisodeId: episodeId,
            NextEpisodeId: episodeId + 1,
            SeriesId: 1,
            Season: 1,
            CompletedAt: DateTime.UtcNow
        );

        await _producer.PublishEpisodeCompletedAsync(evento);

        // Retorna o userId gerado para que o cliente possa usá-lo
        // no GET /episodes/{nextId}/playback-state após o Consumer processar.
        return Accepted(new
        {
            userId        = evento.UserId,
            episodeId     = evento.EpisodeId,
            nextEpisodeId = evento.NextEpisodeId,
            message       = "Evento publicado. Aguarde ~1s e consulte o playback-state do próximo episódio."
        });
    }

    /// <summary>
    /// Retorna onde o player deve iniciar a reprodução.
    /// Consulta o estado pré-computado (via Kafka) ou retorna default do catálogo.
    /// </summary>
    [HttpGet("{episodeId}/playback-state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaybackState(int episodeId, [FromQuery] Guid userId)
    {
        var state = await _episodeService.GetPlaybackStateAsync(userId, episodeId);

        if (state is null)
            return NotFound(new { message = $"Episódio {episodeId} não encontrado no catálogo." });

        return Ok(state);
    }
}
