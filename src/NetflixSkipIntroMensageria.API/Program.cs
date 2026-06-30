// Program.cs -- bootstrap da aplicacao
// Responsabilidade unica: registrar servicos e configurar o pipeline HTTP.
// Nenhuma logica de negocio aqui -- isso vai nos Controllers e Services.

using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Application.Services;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.Infrastructure.Data;
using NetflixSkipIntroMensageria.Infrastructure.Messaging;
using NetflixSkipIntroMensageria.Infrastructure.Repositories;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;
using NetflixSkipIntroMensageria.Streaming.Messaging;

var builder = WebApplication.CreateBuilder(args);

// -- Banco de dados -----------------------------------------------------------
builder.Services.AddDbContext<NetflixDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -- Repositorios (SQL Server) ------------------------------------------------
builder.Services.AddScoped<ICatalogRepository, SqlCatalogRepository>();
builder.Services.AddScoped<IPlaybackStateRepository, SqlPlaybackStateRepository>();

// -- Services (Application layer) --------------------------------------------
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// -- Mensageria (Kafka) -------------------------------------------------------
var kafka = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

// Produtores: Singleton porque IProducer<> e thread-safe e caro de instanciar
builder.Services.AddSingleton(_ => new EpisodeProducer(kafka));
builder.Services.AddSingleton(_ => new PlaybackEventProducer(kafka));

// Consumers: Singleton com IServiceScopeFactory para resolver DbContext (Scoped) por mensagem
builder.Services.AddSingleton(sp => new EpisodeConsumer(kafka, sp.GetRequiredService<IServiceScopeFactory>()));
builder.Services.AddSingleton(sp => new PlaybackConsumer(kafka, sp.GetRequiredService<IServiceScopeFactory>()));

// BackgroundServices que gerenciam o loop de cada consumer
builder.Services.AddHostedService<EpisodeConsumerService>();
builder.Services.AddHostedService<PlaybackConsumerService>();

// -- API ----------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseStaticFiles(); // serve wwwroot/ (inclui /videos/*.mp4)
app.MapControllers();
app.Run();
