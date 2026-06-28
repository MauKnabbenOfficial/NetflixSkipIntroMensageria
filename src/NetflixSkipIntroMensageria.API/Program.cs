//using NetflixSkipIntroMensageria.Infrastructure.Messaging;
//using NetflixSkipIntroMensageria.Streaming.Messaging;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast =  Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

//app.Run();

//record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}


//--------------------------------------------------------------------------------------------------------------------//
//--------------------------------------------------------------------------------------------------------------------//
//--------------------------------------------------------------------------------------------------------------------//

using NetflixSkipIntroMensageria.Infrastructure.Messaging;
using NetflixSkipIntroMensageria.Streaming.Messaging;

var builder = WebApplication.CreateBuilder(args);

var kafka = "localhost:9092";

// Producer disponível para injeção nos controllers
builder.Services.AddSingleton(_ => new EpisodeProducer(kafka));

// Consumer rodando em background
builder.Services.AddSingleton(_ => new EpisodeConsumer(kafka));
builder.Services.AddHostedService<EpisodeConsumerService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// Endpoint que simula o player sinalizando fim de episódio
app.MapPost("/episodes/{episodeId}/completed", async (int episodeId, EpisodeProducer producer) =>
{
    var evento = new NetflixSkipIntroMensageria.SharedKernel.Events.EpisodeCompletedEvent(
        UserId: Guid.NewGuid(),       // depois vira autenticação real
        EpisodeId: episodeId,
        NextEpisodeId: episodeId + 1,
        SeriesId: 1,
        Season: 1,
        CompletedAt: DateTime.UtcNow
    );

    await producer.PublishEpisodeCompletedAsync(evento);
    return Results.Accepted();
});

app.Run();