-- Executado pelo entrypoint do Postgres na primeira subida (volume vazio).
-- Um banco por serviço: apolices e sinistros.

CREATE DATABASE apolices;
CREATE DATABASE sinistros;

-- =====================================================================
-- Banco: apolices
-- =====================================================================
\connect apolices

CREATE TABLE apolice (
    id                 UUID         PRIMARY KEY,
    numero             VARCHAR(30)  NOT NULL,
    segurado_nome      VARCHAR(150) NOT NULL,
    segurado_documento VARCHAR(20)  NOT NULL,
    vigencia_inicio    DATE         NOT NULL,
    vigencia_fim       DATE         NOT NULL,
    versao             INT          NOT NULL DEFAULT 1,   -- trava otimista
    criado_em          TIMESTAMPTZ  NOT NULL,
    atualizado_em      TIMESTAMPTZ  NOT NULL,
    CONSTRAINT uq_apolice_numero   UNIQUE (numero),
    CONSTRAINT ck_apolice_vigencia CHECK (vigencia_fim >= vigencia_inicio)
);

CREATE TABLE apolice_cobertura (
    apolice_id         UUID          NOT NULL,
    tipo               VARCHAR(30)   NOT NULL,
    limite_indenizacao NUMERIC(14,2) NOT NULL,
    CONSTRAINT pk_apolice_cobertura PRIMARY KEY (apolice_id, tipo),
    CONSTRAINT fk_apolice_cobertura_apolice FOREIGN KEY (apolice_id) REFERENCES apolice (id),
    CONSTRAINT ck_apolice_cobertura_limite CHECK (limite_indenizacao > 0)
);

-- Mesmos dados fictícios do antigo repositório em memória
INSERT INTO apolice (id, numero, segurado_nome, segurado_documento, vigencia_inicio, vigencia_fim, versao, criado_em, atualizado_em) VALUES
    ('11111111-1111-1111-1111-111111111111', 'AP-0001', 'Ana Souza',  '111.111.111-11', '2026-01-01', '2026-12-31', 1, NOW(), NOW()),
    ('22222222-2222-2222-2222-222222222222', 'AP-0002', 'Bruno Lima', '222.222.222-22', '2024-01-01', '2024-12-31', 1, NOW(), NOW()),
    ('33333333-3333-3333-3333-333333333333', 'AP-0003', 'Carla Dias', '333.333.333-33', '2026-06-01', '2027-05-31', 1, NOW(), NOW());

INSERT INTO apolice_cobertura (apolice_id, tipo, limite_indenizacao) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Colisao',        50000),
    ('11111111-1111-1111-1111-111111111111', 'Roubo',          30000),
    ('11111111-1111-1111-1111-111111111111', 'Incendio',       40000),
    ('22222222-2222-2222-2222-222222222222', 'Colisao',        20000),
    ('33333333-3333-3333-3333-333333333333', 'Incendio',       15000),
    ('33333333-3333-3333-3333-333333333333', 'DanosTerceiros', 10000);

-- =====================================================================
-- Banco: sinistros
-- =====================================================================
\connect sinistros

-- apolice_id não tem FK: a apólice pertence a outro contexto (outra API/banco).
CREATE TABLE sinistro (
    id              UUID         PRIMARY KEY,
    apolice_id      UUID         NOT NULL,
    data_ocorrencia DATE         NOT NULL,
    tipo_cobertura  VARCHAR(30)  NOT NULL,
    descricao       VARCHAR(500) NOT NULL,
    status          VARCHAR(30)  NOT NULL,
    data_registro   TIMESTAMPTZ  NOT NULL,
    versao          INT          NOT NULL DEFAULT 1,      -- trava otimista
    atualizado_em   TIMESTAMPTZ  NOT NULL
);

CREATE INDEX ix_sinistro_apolice ON sinistro (apolice_id, data_registro DESC);
