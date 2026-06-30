-- ============================================================
-- 02_CreateTables.sql
-- Cria as três tabelas do sistema.
-- Execute após 01_CreateDatabase.sql.
-- ============================================================

USE NetflixSkipIntro;
GO

-- ── Episodes ──────────────────────────────────────────────────────────────────
-- Fonte: Episode.cs + DbContext.OnModelCreating
--   Id: ValueGeneratedNever → sem IDENTITY (PK vem do domínio)
--   Title: HasMaxLength(200) → NVARCHAR(200)
--   VideoStorageKey: HasMaxLength(500) → NVARCHAR(500)
--   Season, Number, IntroStartSeconds, IntroEndSeconds: int → INT NOT NULL
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE Episodes (
    Id                INT           NOT NULL,
    Title             NVARCHAR(200) NOT NULL,
    Season            INT           NOT NULL,
    Number            INT           NOT NULL,
    IntroStartSeconds INT           NOT NULL,
    IntroEndSeconds   INT           NOT NULL,
    VideoStorageKey   NVARCHAR(500) NOT NULL,

    CONSTRAINT PK_Episodes PRIMARY KEY (Id)
);
GO

-- ── PlaybackStates ────────────────────────────────────────────────────────────
-- Fonte: PlaybackStateEntity.cs + DbContext.OnModelCreating
--   Id: ValueGeneratedOnAdd → IDENTITY(1,1)
--   UserId, SessionId: Guid → UNIQUEIDENTIFIER NOT NULL
--   EpisodeId, StartAtSeconds: int → INT NOT NULL
--   VideoStorageKey: HasMaxLength(500) → NVARCHAR(500) NOT NULL
--   ExpiresAt, CreatedAt, UpdatedAt: DateTime → DATETIME2 NOT NULL
--     (defaults GETUTCDATE() são segurança; C# já seta os valores antes de salvar)
--   IsConsumed: bool → BIT NOT NULL DEFAULT 0
--     (default 0 espelha = false do C#)
--   HasIndex(UserId, EpisodeId).IsUnique() → UNIQUE INDEX
--     (garante idempotência do upsert: mesmo evento 2x → sobrescreve, não duplica)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE PlaybackStates (
    Id              INT              NOT NULL IDENTITY(1,1),
    UserId          UNIQUEIDENTIFIER NOT NULL,
    EpisodeId       INT              NOT NULL,
    StartAtSeconds  INT              NOT NULL,
    VideoStorageKey NVARCHAR(500)    NOT NULL,
    SessionId       UNIQUEIDENTIFIER NOT NULL,
    ExpiresAt       DATETIME2        NOT NULL,
    IsConsumed      BIT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_PlaybackStates PRIMARY KEY (Id)
);
GO

CREATE UNIQUE INDEX IX_PlaybackStates_UserId_EpisodeId
    ON PlaybackStates (UserId, EpisodeId);
GO

-- ── WatchProgress ─────────────────────────────────────────────────────────────
-- Fonte: WatchProgressEntity.cs + DbContext.OnModelCreating
--   Id: ValueGeneratedOnAdd → IDENTITY(1,1)
--   UserId, SessionId: Guid → UNIQUEIDENTIFIER NOT NULL
--   EpisodeId, LastPositionSeconds: int → INT NOT NULL
--   UpdatedAt: DateTime → DATETIME2 NOT NULL
--     (default GETUTCDATE() é segurança; C# seta antes de salvar)
--   HasIndex(UserId, EpisodeId).IsUnique() → UNIQUE INDEX
--     (upsert mantém apenas a posição mais recente por usuário/episódio)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE WatchProgress (
    Id                  INT              NOT NULL IDENTITY(1,1),
    UserId              UNIQUEIDENTIFIER NOT NULL,
    EpisodeId           INT              NOT NULL,
    LastPositionSeconds INT              NOT NULL,
    SessionId           UNIQUEIDENTIFIER NOT NULL,
    UpdatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_WatchProgress PRIMARY KEY (Id)
);
GO

CREATE UNIQUE INDEX IX_WatchProgress_UserId_EpisodeId
    ON WatchProgress (UserId, EpisodeId);
GO
