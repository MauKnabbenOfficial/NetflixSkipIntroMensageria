using Confluent.Kafka;
using System.Text.Json;
using NetflixSkipIntroMensageria.SharedKernel.Events;

namespace NetflixSkipIntroMensageria.Streaming.Messaging;

public class EpisodeProducer
{
    private const string Topic = "episode.completed";
    private readonly IProducer<string, string> _producer;

    public EpisodeProducer(string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishEpisodeCompletedAsync(EpisodeCompletedEvent evento)
    {
        var json = JsonSerializer.Serialize(evento);

        var message = new Message<string, string>
        {
            Key = evento.UserId.ToString(), // mesma chave = mesma partição = ordem garantida por usuário
            Value = json
        };

        var result = await _producer.ProduceAsync(Topic, message);
        Console.WriteLine($"Evento publicado: offset {result.Offset}, partição {result.Partition}");
    }

    public void Dispose() => _producer.Dispose();
}