using Microsoft.Extensions.Hosting;

namespace NetflixSkipIntroMensageria.Infrastructure.Messaging;

public class EpisodeConsumerService : BackgroundService
{
    private readonly EpisodeConsumer _consumer;

    public EpisodeConsumerService(EpisodeConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        return Task.Run(() => _consumer.Start(ct), ct);
    }
}