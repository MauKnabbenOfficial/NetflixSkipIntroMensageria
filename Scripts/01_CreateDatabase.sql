-- ============================================================
-- 01_CreateDatabase.sql
-- Cria o banco de dados NetflixSkipIntro.
-- Execute conectado em: localhost,1433 (sa / Netflix@123!)
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'NetflixSkipIntro')
BEGIN
    ALTER DATABASE NetflixSkipIntro SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE NetflixSkipIntro;
    PRINT 'Banco NetflixSkipIntro removido.';
END
GO

CREATE DATABASE NetflixSkipIntro;
PRINT 'Banco NetflixSkipIntro criado.';
GO
