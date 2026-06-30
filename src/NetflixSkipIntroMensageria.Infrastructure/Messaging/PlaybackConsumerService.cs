using Microsoft.Extensions.Hosting;

namespace NetflixSkipIntroMensageria.Infrastructure.Messaging;

public class PlaybackConsumerService : BackgroundService
{
    private readonly PlaybackConsumer _consumer;

    public PlaybackConsumerService(PlaybackConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        return Task.Run(() => _consumer.Start(ct), ct);
    }
}
