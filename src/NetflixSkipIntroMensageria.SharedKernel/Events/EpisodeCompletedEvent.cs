namespace NetflixSkipIntroMensageria.SharedKernel.Events;

public record EpisodeCompletedEvent(
    Guid UserId,
    int EpisodeId,
    int NextEpisodeId,
    int SeriesId,
    int Season,
    DateTime CompletedAt
);

