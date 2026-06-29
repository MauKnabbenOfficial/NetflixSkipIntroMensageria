// ──────────────────────────────────────────────────────────────────────────────
// Program.cs — bootstrap da aplicação
// Responsabilidade única: registrar serviços e configurar o pipeline HTTP.
// Nenhuma lógica de negócio aqui — isso vai nos Controllers e Services.
// ──────────────────────────────────────────────────────────────────────────────

using Microsoft.EntityFrameworkCore;
using NetflixSkipIntroMensageria.Application.Services;
using NetflixSkipIntroMensageria.Catalog.Repositories;
using NetflixSkipIntroMensageria.Infrastructure.Data;
using NetflixSkipIntroMensageria.Infrastructure.Messaging;
using NetflixSkipIntroMensageria.Infrastructure.Repositories;
using NetflixSkipIntroMensageria.SharedKernel.Repositories;
using NetflixSkipIntroMensageria.Streaming.Messaging;

var builder = WebApplication.CreateBuilder(args);

// ── Banco de dados ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NetflixDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositórios (SQL Server) ─────────────────────────────────────────────────
// Troca de InMemory para SQL: só esta seção muda. Controllers e Services
// continuam inalterados — eles conhecem apenas as interfaces.
builder.Services.AddScoped<ICatalogRepository, SqlCatalogRepository>();
builder.Services.AddScoped<IPlaybackStateRepository, SqlPlaybackStateRepository>();

// ── Services (Application layer) ─────────────────────────────────────────────
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// ── Mensageria (Kafka) ────────────────────────────────────────────────────────
var kafka = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

builder.Services.AddSingleton(_ => new EpisodeProducer(kafka));

// EpisodeConsumer recebe IServiceScopeFactory (Singleton) em vez dos repositórios (Scoped).
// Cria um scope por mensagem internamente — padrão correto para BackgroundService + EF Core.
builder.Services.AddSingleton(sp => new EpisodeConsumer(
    kafka,
    sp.GetRequiredService<IServiceScopeFactory>()
));
builder.Services.AddHostedService<EpisodeConsumerService>();

// ── HTTP ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// Serve arquivos estáticos de wwwroot/ — simula o CDN/storage na ausência do S3/Azure Blob.
// O vídeo em wwwroot/videos/episode.mp4 fica acessível em GET /videos/episode.mp4.
// Em produção este middleware não existiria: a API retornaria uma pre-signed URL
// e o player buscaria o vídeo diretamente no storage.
app.UseStaticFiles();

app.MapControllers();

app.Run();
