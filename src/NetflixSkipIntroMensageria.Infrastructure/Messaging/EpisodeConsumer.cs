using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.SharedKernel.Events;
using NetflixSkipIntroMensageria.SharedKernel.Models;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;

namespace NetflixSkipIntroMensageria.Infrastructure.Messaging;

public class EpisodeConsumer
{
    private const string Topic = "episode.completed";
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    // IServiceScopeFactory e Singleton por natureza -- seguro injetar em Singleton.
    // Criamos um scope por mensagem para resolver DbContext (Scoped) corretamente.
    // Padrao obrigatorio para BackgroundService + EF Core.
    public EpisodeConsumer(string bootstrapServers, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = "skip-intro-group",
            AutoOffsetReset  = AutoOffsetReset.Latest,   // Apenas eventos novos -- evita reprocessar stale
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void Start(CancellationToken ct)
    {
        _consumer.Subscribe(Topic);
        Console.WriteLine($"[EpisodeConsumer] Inscrito no topico '{Topic}'. Aguardando eventos...");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var msg    = _consumer.Consume(ct);
                    var evento = JsonSerializer.Deserialize<EpisodeCompletedEvent>(msg.Message.Value);

                    if (evento is not null)
                        ProcessEvent(evento);

                    _consumer.Commit(msg);
                }
                catch (ConsumeException ex) when (!ct.IsCancellationRequested)
                {
                    Console.WriteLine($"[EpisodeConsumer] Erro de consumo: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException) { /* shutdown normal */ }
        finally { _consumer.Close(); }
    }

    private void ProcessEvent(EpisodeCompletedEvent evento)
    {
        using var scope   = _scopeFactory.CreateScope();
        var catalogRepo   = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var playbackRepo  = scope.ServiceProvider.GetRequiredService<IPlaybackStateRepository>();

        var nextEpisode = catalogRepo.GetById(evento.NextEpisodeId);
        if (nextEpisode is null)
        {
            Console.WriteLine($"[EpisodeConsumer] Episodio seguinte {evento.NextEpisodeId} nao encontrado no catalogo.");
            return;
        }

        // Onde o proximo episodio deve comecar: logo apos a intro
        var startAt = nextEpisode.IntroEndSeconds;

        var state = new PlaybackState(
            UserId:          evento.UserId,
            EpisodeId:       evento.NextEpisodeId,
            StartAtSeconds:  startAt,
            VideoStorageKey: nextEpisode.VideoStorageKey,
            SessionId:       evento.SessionId,          // Ancora de sessao
            ExpiresAt:       DateTime.UtcNow.AddHours(2) // TTL de 2h
        );

        playbackRepo.SaveAsync(state).GetAwaiter().GetResult();

        Console.WriteLine($"[EpisodeConsumer] PlaybackState salvo: episodio={evento.NextEpisodeId}, " +
                          $"startAt={startAt}s, sessionId={evento.SessionId}, " +
                          $"expiresAt={state.ExpiresAt:HH:mm:ss}");
    }
}
