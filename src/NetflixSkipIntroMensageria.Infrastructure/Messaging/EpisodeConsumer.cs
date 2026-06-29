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

    // IServiceScopeFactory é Singleton por natureza — seguro injetar em Singleton.
    // Criamos um scope por mensagem para resolver DbContext (Scoped) corretamente.
    // Padrão obrigatório para BackgroundService + EF Core.
    public EpisodeConsumer(string bootstrapServers, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "skip-intro-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void Start(CancellationToken ct)
    {
        _consumer.Subscribe(Topic);
        Console.WriteLine($"[Consumer] Inscrito no tópico '{Topic}'. Aguardando eventos...");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var msg = _consumer.Consume(ct);
                    var evento = JsonSerializer.Deserialize<EpisodeCompletedEvent>(msg.Message.Value);

                    if (evento is null) continue;

                    ProcessarEventoAsync(evento).GetAwaiter().GetResult();
                    _consumer.Commit(msg); // commit manual: só confirma após processar com sucesso
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    // Tópico ainda não existe — o Producer o cria na primeira mensagem.
                    // Aguarda e tenta novamente em vez de crashar a aplicação.
                    Console.WriteLine($"[Consumer] Tópico '{Topic}' ainda não existe. Aguardando 3s...");
                    Thread.Sleep(3000);
                }
                catch (ConsumeException ex)
                {
                    // Outros erros de consumo: loga e continua (não derruba a app).
                    Console.WriteLine($"[Consumer] Erro ao consumir mensagem: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessarEventoAsync(EpisodeCompletedEvent evento)
    {
        // Scope criado por mensagem: DbContext vive só durante o processamento desta mensagem
        // e é descartado ao final — sem vazamento de conexão nem estado compartilhado.
        using var scope = _scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var playbackState = scope.ServiceProvider.GetRequiredService<IPlaybackStateRepository>();

        var proximoEpisodio = catalog.GetById(evento.NextEpisodeId);

        if (proximoEpisodio is null)
        {
            Console.WriteLine($"[Consumer] Ep {evento.NextEpisodeId} não encontrado no catálogo — pode ser o último da série.");
            return;
        }

        var startAt = proximoEpisodio.IntroEndSeconds;

        var state = new PlaybackState(
            UserId: evento.UserId,
            EpisodeId: proximoEpisodio.Id,
            StartAtSeconds: startAt,
            VideoStorageKey: proximoEpisodio.VideoStorageKey
        );

        await playbackState.SaveAsync(state);

        Console.WriteLine(
            $"[Consumer] Usuário {evento.UserId} terminou ep {evento.EpisodeId}. " +
            $"Próximo: '{proximoEpisodio.Title}' (ep {proximoEpisodio.Id}) " +
            $"→ iniciar em {startAt}s (intro: {proximoEpisodio.IntroStartSeconds}s–{proximoEpisodio.IntroEndSeconds}s). " +
            $"Storage: {proximoEpisodio.VideoStorageKey}");
    }
}