# NetflixSkipIntroMensageria

Sistema completo (backend + frontend) simulando o fluxo de **"pular intro"** da Netflix, utilizando mensageria assíncrona com Apache Kafka. Desenvolvido como trabalho final da disciplina de Integração de Aplicações e Dados.

---

## 🌐 URLs de Acesso

| Serviço | URL | Descrição |
|---------|-----|-----------|
| **Frontend (Web)** | **http://localhost:3000** | Interface Vue.js do player |
| **API Backend** | **http://localhost:5091** | REST API .NET |
| **Kafka UI** | **http://localhost:8090** | Monitoramento do Kafka |
| **SQL Server** | **localhost,1433** | Banco de dados (sa/Netflix@123!) |

---

## Sobre o projeto

O sistema possui um **frontend Vue.js** que simula um player de vídeo tipo Netflix e um **backend .NET** com mensageria Kafka.

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

### Backend (.NET 10)

| Projeto | Responsabilidade |
|---|---|
| `SharedKernel` | Eventos de domínio, modelos e interfaces de repositório |
| `Catalog` | Entidade `Episode` e interface `ICatalogRepository` |
| `Application` | Services (`EpisodeService`, `CatalogService`) e DTOs |
| `Infrastructure` | EF Core, `DbContext`, repositórios SQL Server, Consumer Kafka |
| `Streaming` | `EpisodeProducer` — publicação de eventos no Kafka |
| `API` | Controllers REST (`EpisodesController`, `CatalogController`) |

### Frontend (Vue.js 3)

| Tecnologia | Versão |
|---|---|
| Vue.js | ^3.4.0 |
| Vite | ^5.0.0 |
| @vitejs/plugin-vue | ^5.0.0 |

O frontend roda na porta **3000** e faz proxy das requisições para a API backend.

---

## Pré-requisitos

### Backend
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup) — opcional, para inspecionar o banco
- Visual Studio 2022 v17.6+ (ou VS Code)

### Frontend
- [Node.js 18+](https://nodejs.org/) com npm
- Qualquer navegador moderno (Chrome, Edge, Firefox)

### Pacotes e Versões do Backend

| Pacote NuGet | Versão | Projeto |
|---|---|---|
| `Confluent.Kafka` | 2.14.2 | Infrastructure, Streaming |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.0 | Infrastructure |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.0 | Infrastructure |
| `Microsoft.Extensions.Hosting.Abstractions` | 10.0.9 | Infrastructure |
| `Microsoft.AspNetCore.OpenApi` | 10.0.8 | API |

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

### 5. Inicie a API (Backend)

Abra a solution no Visual Studio e pressione **F5** com o projeto `NetflixSkipIntroMensageria.API` como startup.

A API estará disponível em: **http://localhost:5091**

### 6. Inicie o Frontend (Web)

Abra um novo terminal na pasta `web`:

```bash
cd web
npm install           # Instala as dependências (apenas primeira vez)
npm run dev           # Inicia o servidor de desenvolvimento
```

O frontend estará disponível em: **http://localhost:3000**

**🎯 Acesse http://localhost:3000 no navegador para usar o sistema!**

---

## Testando

### Via Interface Web (Recomendado)

1. Acesse **http://localhost:3000**
2. Selecione um episódio
3. Assista e clique em "Próximo Episódio"
4. O próximo episódio iniciará automaticamente após a intro (70s)

### Via API REST (Testes manuais)

Use o arquivo `NetflixSkipIntroMensageria.API.http` no Visual Studio ou importe no Postman.

**URL Base da API:** http://localhost:5091

#### Fluxo completo

**1. Verificar catálogo**
```
GET http://localhost:5091/catalog/episodes
```

**2. Sinalizar fim do episódio 1** — anote o `userId` da resposta
```
POST http://localhost:5091/episodes/1/completed
```

**3. Consultar onde o ep 2 deve iniciar** — aguarde ~1s
```
GET http://localhost:5091/episodes/2/playback-state?userId={userId-do-passo-2}
```
Resposta esperada: `startAtSeconds: 70, source: "pre-computed"`

**4. Acesso direto** (sem ter vindo do ep anterior)
```
GET http://localhost:5091/episodes/3/playback-state?userId=00000000-0000-0000-0000-000000000000
```
Resposta esperada: `startAtSeconds: 0, source: "catalog-default"`

---

## Padrões e técnicas aplicadas

### Backend
- **Event-driven architecture** com Apache Kafka
- **Repository Pattern** — interfaces no domínio, implementações na Infrastructure
- **CQRS simplificado** — escrita via evento Kafka, leitura via endpoint REST
- **IServiceScopeFactory** no Consumer para resolver `DbContext` (Scoped) dentro de um Singleton
- **Commit manual no Kafka** (`EnableAutoCommit=false`) — garante *at-least-once delivery*
- **Idempotência** — índice único `(UserId, EpisodeId)` em `PlaybackStates`
- **Fallback** no `GET playback-state`: retorna estado pré-computado ou default do catálogo
- **Static files** servindo o vídeo localmente, simulando um CDN/storage

### Frontend
- **Vue.js 3 Composition API** — reatividade moderna e performance otimizada
- **Vite** — build tool com HMR (Hot Module Replacement) instantâneo
- **Proxy reverso** — requisições `/api` e `/videos` redirecionadas ao backend
- **Controle de playback** — integração com eventos de vídeo HTML5

---

## Monitoramento e Ferramentas

| Serviço | Porta | URL | Credenciais |
|---|---|---|---|
| **Frontend (Vue)** | **3000** | **http://localhost:3000** | — |
| **API Backend** | **5091** | **http://localhost:5091** | — |
| **Kafka** | 9092 | — | — |
| **Kafka UI** | **8090** | **http://localhost:8090** | — |
| **SQL Server** | **1433** | **localhost,1433** | sa / Netflix@123! |

### Como visualizar as mensagens no Kafka

1. Acesse **http://localhost:8090**
2. Navegue até **Topics**
3. Selecione `episode.completed` ou `playback.computed`
4. Visualize as mensagens em tempo real
