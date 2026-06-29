# NetflixSkipIntroMensageria

Backend simulando o fluxo de **"pular intro"** da Netflix, utilizando mensageria assíncrona com Apache Kafka. Desenvolvido como trabalho final da disciplina de Integração de Aplicações e Dados.

---

## Sobre o projeto

Quando o usuário termina um episódio e clica em **"Próximo episódio"**, o player emite um evento via Kafka. Um Consumer processa esse evento de forma assíncrona, consulta os metadados do próximo episódio (tempo de intro) e persiste o estado no banco. Quando o próximo episódio inicia, a API informa ao player exatamente em quantos segundos começar — pulando a intro automaticamente.

```
POST /episodes/{id}/completed
        ↓
   Kafka (episode.completed)
        ↓
   Consumer → consulta Episodes (SQL)
           → salva PlaybackStates (SQL)

GET /episodes/{id}/playback-state?userId=...
        ↓
   { startAtSeconds: 70, source: "pre-computed" }
```

---

## Arquitetura

| Projeto | Responsabilidade |
|---|---|
| `SharedKernel` | Eventos de domínio, modelos e interfaces de repositório |
| `Catalog` | Entidade `Episode` e interface `ICatalogRepository` |
| `Application` | Services (`EpisodeService`, `CatalogService`) e DTOs |
| `Infrastructure` | EF Core, `DbContext`, repositórios SQL Server, Consumer Kafka |
| `Streaming` | `EpisodeProducer` — publicação de eventos no Kafka |
| `API` | Controllers REST (`EpisodesController`, `CatalogController`) |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup) — opcional, para inspecionar o banco
- Visual Studio 2022 v17.6+ (ou VS Code)

---

## Setup

### 1. Clone o repositório

```bash
git clone https://github.com/MauKnabbenOfficial/NetflixSkipIntroMensageria.git
cd NetflixSkipIntroMensageria
```

### 2. Adicione o vídeo de demonstração

O vídeo **não está no repositório** (arquivo binário grande). Coloque qualquer vídeo `.mp4` em:

```
src/NetflixSkipIntroMensageria.API/wwwroot/videos/GTAVI.mp4
```

O nome deve ser exatamente `GTAVI.mp4`. A intro do vídeo utilizado vai de `0s` até `70s` (1:10 min).

### 3. Suba a infraestrutura com Docker

```bash
docker compose up zookeeper kafka kafka-ui sqlserver -d
```

Aguarde ~30 segundos para o SQL Server inicializar completamente.

### 4. Crie o banco de dados

Conecte no SSMS com:
- **Server:** `localhost,1433`
- **Authentication:** SQL Server Authentication
- **Login:** `sa` / **Password:** `Netflix@123!`

Execute os scripts da pasta `Scripts/` em ordem:

```
01_CreateDatabase.sql
02_CreateTables.sql
03_Seed_Episodes.sql
```

### 5. Inicie a API

Abra a solution no Visual Studio e pressione **F5** com o projeto `NetflixSkipIntroMensageria.API` como startup.

---

## Testando

Use o arquivo `NetflixSkipIntroMensageria.API.http` no Visual Studio ou importe no Postman.

### Fluxo completo

**1. Verificar catálogo**
```
GET /catalog/episodes
```

**2. Sinalizar fim do episódio 1** — anote o `userId` da resposta
```
POST /episodes/1/completed
```

**3. Consultar onde o ep 2 deve iniciar** — aguarde ~1s
```
GET /episodes/2/playback-state?userId={userId-do-passo-2}
```
Resposta esperada: `startAtSeconds: 70, source: "pre-computed"`

**4. Acesso direto** (sem ter vindo do ep anterior)
```
GET /episodes/3/playback-state?userId=00000000-0000-0000-0000-000000000000
```
Resposta esperada: `startAtSeconds: 0, source: "catalog-default"`

---

## Padrões e técnicas aplicadas

- **Event-driven architecture** com Apache Kafka
- **Repository Pattern** — interfaces no domínio, implementações na Infrastructure
- **CQRS simplificado** — escrita via evento Kafka, leitura via endpoint REST
- **IServiceScopeFactory** no Consumer para resolver `DbContext` (Scoped) dentro de um Singleton
- **Commit manual no Kafka** (`EnableAutoCommit=false`) — garante *at-least-once delivery*
- **Idempotência** — índice único `(UserId, EpisodeId)` em `PlaybackStates`
- **Fallback** no `GET playback-state`: retorna estado pré-computado ou default do catálogo
- **Static files** servindo o vídeo localmente, simulando um CDN/storage

---

## Serviços Docker

| Serviço | Porta | URL |
|---|---|---|
| Kafka | 9092 | — |
| Kafka UI | 8090 | http://localhost:8090 |
| SQL Server | 1433 | localhost,1433 |
| API | 5000 | http://localhost:5000 |
