-- ============================================================
-- 02_CreateTables.sql
-- Cria as tabelas Episodes e PlaybackStates.
-- Execute após 01_CreateDatabase.sql.
-- ============================================================

USE NetflixSkipIntro;
GO

-- ── Episodes ──────────────────────────────────────────────
-- Catálogo de episódios. Em produção viria de um serviço separado,
-- mas aqui centralizamos no mesmo banco para simplificar.
-- VideoStorageKey: caminho relativo no storage (S3, Azure Blob, etc.)
-- Todos os episódios apontam para o mesmo arquivo físico nesta simulação.
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Episodes')
BEGIN
    CREATE TABLE Episodes (
        Id                INT             NOT NULL,
        Title             NVARCHAR(200)   NOT NULL,
        Season            INT             NOT NULL,
        Number            INT             NOT NULL,
        IntroStartSeconds INT             NOT NULL DEFAULT 0,
        IntroEndSeconds   INT             NOT NULL DEFAULT 0,
        VideoStorageKey   NVARCHAR(500)   NOT NULL,

        CONSTRAINT PK_Episodes PRIMARY KEY (Id)
    );
    PRINT 'Tabela Episodes criada.';
END
ELSE
BEGIN
    PRINT 'Tabela Episodes já existe — pulando criação.';
END
GO

-- ── PlaybackStates ────────────────────────────────────────
-- Estado de reprodução pré-computado pelo Consumer Kafka.
-- Registra onde cada usuário deve iniciar o próximo episódio (após a intro).
-- Índice único em (UserId, EpisodeId) garante idempotência:
-- o mesmo evento processado duas vezes sobrescreve, não duplica.
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlaybackStates')
BEGIN
    CREATE TABLE PlaybackStates (
        Id               INT              NOT NULL IDENTITY(1,1),
        UserId           UNIQUEIDENTIFIER NOT NULL,
        EpisodeId        INT              NOT NULL,
        StartAtSeconds   INT              NOT NULL DEFAULT 0,
        VideoStorageKey  NVARCHAR(500)    NOT NULL,
        CreatedAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_PlaybackStates PRIMARY KEY (Id),
        CONSTRAINT FK_PlaybackStates_Episodes FOREIGN KEY (EpisodeId)
            REFERENCES Episodes(Id),
        CONSTRAINT UQ_PlaybackStates_User_Episode UNIQUE (UserId, EpisodeId)
    );
    PRINT 'Tabela PlaybackStates criada.';
END
ELSE
BEGIN
    PRINT 'Tabela PlaybackStates já existe — pulando criação.';
END
GO
