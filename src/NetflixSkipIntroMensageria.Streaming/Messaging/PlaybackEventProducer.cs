using Confluent.Kafka;
using System.Text.Json;
using NetflixSkipIntroMensageria.SharedKernel.Events;

namespace NetflixSkipIntroMensageria.Streaming.Messaging;

/// <summary>
/// Produtor para eventos de ciclo de vida do player:
///   playback.started  — player iniciou reprodução (dispara marcação de IsConsumed)
///   playback.position — heartbeat de progresso a cada 30s (alimenta "continue watching")
///
/// Chave = UserId → mesma partição = ordem garantida por usuário.
/// </summary>
public class PlaybackEventProducer
{
    private const string TopicStarted  = "playback.started";
    private const string TopicPosition = "playback.position";

    private readonly IProducer<string, string> _producer;

    public PlaybackEventProducer(string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishPlaybackStartedAsync(PlaybackStartedEvent evento)
    {
        var json = JsonSerializer.Serialize(evento);
        var message = new Message<string, string> { Key = evento.UserId.ToString(), Value = json };
        var result = await _producer.ProduceAsync(TopicStarted, message);
        Console.WriteLine($"[PlaybackEventProducer] playback.started publicado: offset {result.Offset}");
    }

    public async Task PublishPlaybackPositionAsync(PlaybackPositionEvent evento)
    {
        var json = JsonSerializer.Serialize(evento);
        var message = new Message<string, string> { Key = evento.UserId.ToString(), Value = json };
        var result = await _producer.ProduceAsync(TopicPosition, message);
        Console.WriteLine($"[PlaybackEventProducer] playback.position publicado: offset {result.Offset}, pos={evento.PositionSeconds}s");
    }

    public void Dispose() => _producer.Dispose();
}
