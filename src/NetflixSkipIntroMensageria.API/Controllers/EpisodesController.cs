using Microsoft.AspNetCore.Mvc;
using NetflixSkipIntroMensageria.Application.Services;
using NetflixSkipIntroMensageria.SharedKernel.Events;
using NetflixSkipIntroMensageria.Streaming.Messaging;

namespace NetflixSkipIntroMensageria.API.Controllers;

[ApiController]
[Route("episodes")]
public class EpisodesController : ControllerBase
{
    private readonly EpisodeProducer _episodeProducer;
    private readonly PlaybackEventProducer _playbackProducer;
    private readonly IEpisodeService _episodeService;

    public EpisodesController(
        EpisodeProducer episodeProducer,
        PlaybackEventProducer playbackProducer,
        IEpisodeService episodeService)
    {
        _episodeProducer  = episodeProducer;
        _playbackProducer = playbackProducer;
        _episodeService   = episodeService;
    }

    /// <summary>
    /// Sinaliza que o usuario terminou o episodio (automatico ou clicou em proximo).
    /// Publica EpisodeCompletedEvent com SessionId — o Consumer so aceita eventos desta sessao.
    /// </summary>
    [HttpPost("{episodeId}/completed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> CompleteEpisode(
        int episodeId,
        [FromQuery] Guid userId,
        [FromQuery] Guid sessionId)
    {
        var evento = new EpisodeCompletedEvent(
            UserId:        userId    == Guid.Empty ? Guid.NewGuid() : userId,
            EpisodeId:     episodeId,
            NextEpisodeId: episodeId + 1,
            SeriesId:      1,
            Season:        1,
            CompletedAt:   DateTime.UtcNow,
            SessionId:     sessionId == Guid.Empty ? Guid.NewGuid() : sessionId
        );

        await _episodeProducer.PublishEpisodeCompletedAsync(evento);

        return Accepted(new
        {
            userId        = evento.UserId,
            sessionId     = evento.SessionId,
            episodeId     = evento.EpisodeId,
            nextEpisodeId = evento.NextEpisodeId,
            message       = "Evento publicado. Aguarde ~1s e consulte o playback-state do proximo episodio."
        });
    }

    /// <summary>
    /// Retorna onde o player deve iniciar a reproducao para esta sessao especifica.
    /// Rejeita automaticamente: estados de sessoes anteriores, expirados (>2h) ou ja consumidos.
    /// </summary>
    [HttpGet("{episodeId}/playback-state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaybackState(
        int episodeId,
        [FromQuery] Guid userId,
        [FromQuery] Guid sessionId)
    {
        var state = await _episodeService.GetPlaybackStateAsync(userId, episodeId, sessionId);

        if (state is null)
            return NotFound(new { message = $"Episodio {episodeId} nao encontrado no catalogo." });

        return Ok(state);
    }

    /// <summary>
    /// Confirmacao de que o player iniciou a reproducao.
    /// Publica PlaybackStartedEvent → Consumer marca PlaybackState.IsConsumed = true.
    /// Impede que o mesmo skip seja aplicado numa segunda abertura do episodio.
    /// </summary>
    [HttpPost("{episodeId}/playback-started")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PlaybackStarted(
        int episodeId,
        [FromBody] PlaybackStartedRequest req)
    {
        var evento = new PlaybackStartedEvent(
            UserId:         req.UserId,
            EpisodeId:      episodeId,
            StartAtSeconds: req.StartAtSeconds,
            SessionId:      req.SessionId,
            StartedAt:      DateTime.UtcNow);

        await _playbackProducer.PublishPlaybackStartedAsync(evento);

        return Accepted(new
        {
            message = $"PlaybackStartedEvent publicado para episodio {episodeId}, sessionId={req.SessionId}"
        });
    }

    /// <summary>
    /// Heartbeat de posicao enviado a cada 30s enquanto o usuario assiste.
    /// Publica PlaybackPositionEvent → Consumer grava WatchProgress para "continue watching".
    /// </summary>
    [HttpPost("{episodeId}/position")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReportPosition(
        int episodeId,
        [FromBody] PlaybackPositionRequest req)
    {
        var evento = new PlaybackPositionEvent(
            UserId:          req.UserId,
            EpisodeId:       episodeId,
            PositionSeconds: req.PositionSeconds,
            SessionId:       req.SessionId,
            OccurredAt:      DateTime.UtcNow);

        await _playbackProducer.PublishPlaybackPositionAsync(evento);

        return Accepted(new { message = $"Posicao {req.PositionSeconds}s registrada para episodio {episodeId}." });
    }
}

// ── Request bodies ────────────────────────────────────────────────────────────
public record PlaybackStartedRequest(Guid UserId, int StartAtSeconds, Guid SessionId);
public record PlaybackPositionRequest(Guid UserId, int PositionSeconds, Guid SessionId);
