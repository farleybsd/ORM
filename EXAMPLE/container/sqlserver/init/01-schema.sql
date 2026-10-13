-- Executado pelo container sqlserver-init a cada "docker compose up".
-- Idempotente: só cria o que ainda não existe.
-- Um banco por serviço: apolices e sinistros.

IF DB_ID('apolices') IS NULL CREATE DATABASE apolices;
GO
IF DB_ID('sinistros') IS NULL CREATE DATABASE sinistros;
GO

-- =====================================================================
-- Banco: apolices
-- =====================================================================
USE apolices;
GO

-- Leituras não bloqueiam escritas (comportamento parecido com o MVCC do Postgres)
ALTER DATABASE apolices SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

IF OBJECT_ID('dbo.apolice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.apolice (
        id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_apolice PRIMARY KEY NONCLUSTERED,
        numero             VARCHAR(30)      NOT NULL,
        segurado_nome      NVARCHAR(150)    NOT NULL,
        segurado_documento VARCHAR(20)      NOT NULL,
        vigencia_inicio    DATE             NOT NULL,
        vigencia_fim       DATE             NOT NULL,
        versao             INT              NOT NULL CONSTRAINT df_apolice_versao DEFAULT 1, -- trava otimista
        criado_em          DATETIME2(3)     NOT NULL,
        atualizado_em      DATETIME2(3)     NOT NULL,
        CONSTRAINT uq_apolice_numero   UNIQUE (numero),
        CONSTRAINT ck_apolice_vigencia CHECK (vigencia_fim >= vigencia_inicio)
    );

    CREATE TABLE dbo.apolice_cobertura (
        apolice_id         UNIQUEIDENTIFIER NOT NULL,
        tipo               VARCHAR(30)      NOT NULL,
        limite_indenizacao DECIMAL(14,2)    NOT NULL,
        CONSTRAINT pk_apolice_cobertura PRIMARY KEY (apolice_id, tipo),
        CONSTRAINT fk_apolice_cobertura_apolice FOREIGN KEY (apolice_id) REFERENCES dbo.apolice (id),
        CONSTRAINT ck_apolice_cobertura_limite CHECK (limite_indenizacao > 0)
    );

    -- Mesmos dados fictícios do antigo repositório em memória
    INSERT INTO dbo.apolice (id, numero, segurado_nome, segurado_documento, vigencia_inicio, vigencia_fim, versao, criado_em, atualizado_em) VALUES
        ('11111111-1111-1111-1111-111111111111', 'AP-0001', N'Ana Souza',  '111.111.111-11', '2026-01-01', '2026-12-31', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        ('22222222-2222-2222-2222-222222222222', 'AP-0002', N'Bruno Lima', '222.222.222-22', '2024-01-01', '2024-12-31', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        ('33333333-3333-3333-3333-333333333333', 'AP-0003', N'Carla Dias', '333.333.333-33', '2026-06-01', '2027-05-31', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.apolice_cobertura (apolice_id, tipo, limite_indenizacao) VALUES
        ('11111111-1111-1111-1111-111111111111', 'Colisao',        50000),
        ('11111111-1111-1111-1111-111111111111', 'Roubo',          30000),
        ('11111111-1111-1111-1111-111111111111', 'Incendio',       40000),
        ('22222222-2222-2222-2222-222222222222', 'Colisao',        20000),
        ('33333333-3333-3333-3333-333333333333', 'Incendio',       15000),
        ('33333333-3333-3333-3333-333333333333', 'DanosTerceiros', 10000);
END
GO

-- =====================================================================
-- Banco: sinistros
-- =====================================================================
USE sinistros;
GO

ALTER DATABASE sinistros SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

IF OBJECT_ID('dbo.sinistro', 'U') IS NULL
BEGIN
    -- apolice_id não tem FK: a apólice pertence a outro contexto (outra API/banco).
    CREATE TABLE dbo.sinistro (
        id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_sinistro PRIMARY KEY NONCLUSTERED,
        apolice_id      UNIQUEIDENTIFIER NOT NULL,
        data_ocorrencia DATE             NOT NULL,
        tipo_cobertura  VARCHAR(30)      NOT NULL,
        descricao       NVARCHAR(500)    NOT NULL,
        status          VARCHAR(30)      NOT NULL,
        data_registro   DATETIME2(3)     NOT NULL,
        versao          INT              NOT NULL CONSTRAINT df_sinistro_versao DEFAULT 1, -- trava otimista
        atualizado_em   DATETIME2(3)     NOT NULL
    );

    CREATE INDEX ix_sinistro_apolice ON dbo.sinistro (apolice_id, data_registro DESC);
END
GO
