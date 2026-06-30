using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NetflixSkipIntroMensageria.Infrastructure.Data;
using NetflixSkipIntroMensageria.Infrastructure.Data.Entities;
using NetflixSkipIntroMensageria.SharedKernel.Events;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Messaging;

/// <summary>
/// Consumer responsável pela integridade do estado de reprodução.
///
/// Tópicos assinados:
///   playback.started  → marca PlaybackState como IsConsumed = true
///                        impede que o mesmo skip seja aplicado numa segunda abertura
///   playback.position → upsert em WatchProgress (userId, episodeId, lastPosition)
///                        alimenta "continue watching" em sessões futuras
/// </summary>
public class PlaybackConsumer
{
    private const string GroupId        = "playback-integrity-group";
    private const string TopicStarted   = "playback.started";
    private const string TopicPosition  = "playback.position";

    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public PlaybackConsumer(string bootstrapServers, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = GroupId,
            AutoOffsetReset  = AutoOffsetReset.Latest,  // Só eventos novos — posição de ontem não importa
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void Start(CancellationToken ct)
    {
        _consumer.Subscribe(new[] { TopicStarted, TopicPosition });
        Console.WriteLine($"[PlaybackConsumer] Inscrito em '{TopicStarted}' e '{TopicPosition}'.");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var msg = _consumer.Consume(ct);
                    var topic = msg.Topic;

                    if (topic == TopicStarted)
                        ProcessStarted(msg.Message.Value);
                    else if (topic == TopicPosition)
                        ProcessPosition(msg.Message.Value);

                    _consumer.Commit(msg);
                }
                catch (ConsumeException ex) when (!ct.IsCancellationRequested)
                {
                    Console.WriteLine($"[PlaybackConsumer] Erro de consumo: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException) { /* shutdown normal */ }
        finally { _consumer.Close(); }
    }

    // ── playback.started ──────────────────────────────────────────────────────
    private void ProcessStarted(string json)
    {
        var evento = JsonSerializer.Deserialize<PlaybackStartedEvent>(json);
        if (evento is null) return;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPlaybackStateRepository>();

        repo.MarkConsumedAsync(evento.UserId, evento.EpisodeId).GetAwaiter().GetResult();

        Console.WriteLine($"[PlaybackConsumer] PlaybackState marcado como consumido — " +
                          $"userId={evento.UserId}, episodeId={evento.EpisodeId}, sessionId={evento.SessionId}");
    }

    // ── playback.position ────────────────────────────────────────────────────
    private void ProcessPosition(string json)
    {
        var evento = JsonSerializer.Deserialize<PlaybackPositionEvent>(json);
        if (evento is null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NetflixDbContext>();

        // Upsert: mantém apenas o progresso mais recente para (userId, episodeId)
        var existing = db.WatchProgress
            .FirstOrDefault(x => x.UserId == evento.UserId && x.EpisodeId == evento.EpisodeId);

        if (existing is null)
        {
            db.WatchProgress.Add(new WatchProgressEntity
            {
                UserId              = evento.UserId,
                EpisodeId           = evento.EpisodeId,
                LastPositionSeconds = evento.PositionSeconds,
                SessionId           = evento.SessionId,
                UpdatedAt           = evento.OccurredAt
            });
        }
        else
        {
            existing.LastPositionSeconds = evento.PositionSeconds;
            existing.SessionId           = evento.SessionId;
            existing.UpdatedAt           = evento.OccurredAt;
        }

        db.SaveChanges();

        Console.WriteLine($"[PlaybackConsumer] WatchProgress atualizado — " +
                          $"userId={evento.UserId}, episodeId={evento.EpisodeId}, pos={evento.PositionSeconds}s");
    }
}
