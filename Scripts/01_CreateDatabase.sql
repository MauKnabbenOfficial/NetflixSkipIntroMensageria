-- ============================================================
-- 01_CreateDatabase.sql
-- Cria o banco de dados NetflixSkipIntro.
-- Execute conectado em: localhost,1433 (sa / Netflix@123!)
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NetflixSkipIntro')
BEGIN
    CREATE DATABASE NetflixSkipIntro;
    PRINT 'Banco NetflixSkipIntro criado.';
END
ELSE
BEGIN
    PRINT 'Banco NetflixSkipIntro já existe — pulando criação.';
END
GO
