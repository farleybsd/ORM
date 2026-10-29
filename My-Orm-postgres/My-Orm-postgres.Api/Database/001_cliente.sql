-- Tabela usada pelo model Cliente (My-Orm-Data-Access/Models/Cliente.cs)
-- inserted_at / updated_at: unix em milissegundos, preenchidos pelo ORM.
-- updated_at também é a "versao" da concorrência otimista.
CREATE TABLE IF NOT EXISTS cliente (
    id          BIGSERIAL    PRIMARY KEY,
    nome        VARCHAR(150) NOT NULL,
    email       VARCHAR(200) NOT NULL UNIQUE,
    ativo       BOOLEAN      NOT NULL DEFAULT TRUE,
    inserted_at BIGINT       NOT NULL,
    updated_at  BIGINT       NOT NULL
);
