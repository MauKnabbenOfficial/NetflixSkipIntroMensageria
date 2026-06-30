-- ============================================================
-- 03_Seed_Episodes.sql
-- Insere os 5 episódios de seed no catálogo.
-- Execute após 02_CreateTables.sql.
--
-- Todos os episódios apontam para o mesmo arquivo físico (GTAVI.mp4).
-- Intro: 0s–70s nos eps 1–4. Ep 5 (Finale) sem intro.
-- ============================================================

USE NetflixSkipIntro;
GO

INSERT INTO Episodes (Id, Title, Season, Number, IntroStartSeconds, IntroEndSeconds, VideoStorageKey)
VALUES
    (1, 'Piloto',      1, 1, 0, 70, 'videos/GTAVI.mp4'),
    (2, 'O Começo',    1, 2, 0, 70, 'videos/GTAVI.mp4'),
    (3, 'A Virada',    1, 3, 0, 70, 'videos/GTAVI.mp4'),
    (4, 'O Confronto', 1, 4, 0, 70, 'videos/GTAVI.mp4'),
    (5, 'Finale',      1, 5, 0,  0, 'videos/GTAVI.mp4');
GO

PRINT '5 episódios inseridos.';
GO
