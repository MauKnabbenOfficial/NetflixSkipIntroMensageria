using Confluent.Kafka;
using System.Text.Json;
using NetflixSkipIntroMensageria.SharedKernel.Events;

namespace NetflixSkipIntroMensageria.Infrastructure.Messaging;

public class EpisodeConsumer
{
    private const string Topic = "episode.completed";
    private readonly IConsumer<string, string> _consumer;

    public EpisodeConsumer(string bootstrapServers)
    {
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
        Console.WriteLine("Consumer aguardando eventos...");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var msg = _consumer.Consume(ct);
                var evento = JsonSerializer.Deserialize<EpisodeCompletedEvent>(msg.Message.Value);

                if (evento is null) continue;

                ProcessarEvento(evento);
                _consumer.Commit(msg);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _consumer.Close();
        }
    }

    private void ProcessarEvento(EpisodeCompletedEvent evento)
    {
        // Por enquanto só loga — aqui vai entrar o SQL Server depois
        Console.WriteLine($"[Consumer] Usuário {evento.UserId} terminou ep {evento.EpisodeId}. " +
                          $"Próximo: ep {evento.NextEpisodeId} (T{evento.Season})");
    }
}