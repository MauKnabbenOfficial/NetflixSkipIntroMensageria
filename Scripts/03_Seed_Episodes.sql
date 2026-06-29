-- ============================================================
-- 03_Seed_Episodes.sql
-- Insere os 5 episódios de seed no catálogo.
-- Execute após 02_CreateTables.sql.
--
-- Vídeo de demonstração: GTAVI.mp4 (2:49 min)
-- Intro: 0s → 70s (1:10 min) para episódios 1 a 4.
-- Episódio 5 (Finale) não tem intro.
-- ============================================================

USE NetflixSkipIntro;
GO

IF NOT EXISTS (SELECT 1 FROM Episodes)
BEGIN
    INSERT INTO Episodes (Id, Title, Season, Number, IntroStartSeconds, IntroEndSeconds, VideoStorageKey)
    VALUES
        (1, 'Piloto',      1, 1,  0, 70, 'videos/GTAVI.mp4'),
        (2, 'O Começo',    1, 2,  0, 70, 'videos/GTAVI.mp4'),
        (3, 'A Virada',    1, 3,  0, 70, 'videos/GTAVI.mp4'),
        (4, 'O Confronto', 1, 4,  0, 70, 'videos/GTAVI.mp4'),
        (5, 'Finale',      1, 5,  0,  0, 'videos/GTAVI.mp4');

    PRINT '5 episódios inseridos com sucesso.';
END
ELSE
BEGIN
    -- Banco já possui dados: atualiza para garantir consistência com o vídeo atual.
    UPDATE Episodes SET IntroStartSeconds = 0, IntroEndSeconds = 70, VideoStorageKey = 'videos/GTAVI.mp4' WHERE Id IN (1,2,3,4);
    UPDATE Episodes SET IntroStartSeconds = 0, IntroEndSeconds = 0,  VideoStorageKey = 'videos/GTAVI.mp4' WHERE Id = 5;
    PRINT 'Episódios atualizados com tempos e nome de vídeo corretos.';
END
GO
