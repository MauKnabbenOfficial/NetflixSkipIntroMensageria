namespace NetflixSkipIntroMensageria.Application.DTOs;

public record EpisodeDto(
    int Id,
    string Title,
    int Season,
    int Number,
    int IntroStartSeconds,
    int IntroEndSeconds,
    string VideoStorageKey
);
