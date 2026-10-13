-- Rode no banco "loja" do PostgreSQL
CREATE TABLE cliente (
    id          BIGSERIAL    PRIMARY KEY,
    nome        VARCHAR(100) NOT NULL,
    email       VARCHAR(150) NOT NULL UNIQUE,
    ativo       BOOLEAN      NOT NULL DEFAULT TRUE,
    inserted_at BIGINT       NOT NULL DEFAULT 0,
    updated_at  BIGINT       NOT NULL DEFAULT 0
);
CREATE TABLE pedido (
    id         BIGSERIAL PRIMARY KEY,
    cliente_id BIGINT NOT NULL REFERENCES cliente(id),
    total      NUMERIC(12,2) NOT NULL
);

CREATE TABLE pedido_item (
    id        BIGSERIAL PRIMARY KEY,
    pedido_id BIGINT NOT NULL REFERENCES pedido(id),
    produto   VARCHAR(100) NOT NULL,
    valor     NUMERIC(12,2) NOT NULL
);

-- Capítulo 15: tabela com nome fora do padrão
CREATE TABLE tb_fornecedor_legado (
    id           BIGSERIAL    PRIMARY KEY,
    razao_social VARCHAR(100) NOT NULL,
    inserted_at  BIGINT       NOT NULL DEFAULT 0,
    updated_at   BIGINT       NOT NULL DEFAULT 0
);
