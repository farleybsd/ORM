-- Rode no banco "loja" do MySQL (capítulo 16)
CREATE TABLE cliente (
    id          BIGINT       AUTO_INCREMENT PRIMARY KEY,
    nome        VARCHAR(100) NOT NULL,
    email       VARCHAR(150) NOT NULL UNIQUE,
    ativo       BOOLEAN      NOT NULL DEFAULT TRUE,
    inserted_at BIGINT       NOT NULL DEFAULT 0,
    updated_at  BIGINT       NOT NULL DEFAULT 0
);

CREATE TABLE pedido (
    id         BIGINT AUTO_INCREMENT PRIMARY KEY,
    cliente_id BIGINT NOT NULL,
    total      DECIMAL(12,2) NOT NULL,
    FOREIGN KEY (cliente_id) REFERENCES cliente(id)
);

CREATE TABLE pedido_item (
    id        BIGINT AUTO_INCREMENT PRIMARY KEY,
    pedido_id BIGINT NOT NULL,
    produto   VARCHAR(100) NOT NULL,
    valor     DECIMAL(12,2) NOT NULL,
    FOREIGN KEY (pedido_id) REFERENCES pedido(id)
);

CREATE TABLE tb_fornecedor_legado (
    id           BIGINT AUTO_INCREMENT PRIMARY KEY,
    razao_social VARCHAR(100) NOT NULL,
    inserted_at  BIGINT       NOT NULL DEFAULT 0,
    updated_at   BIGINT       NOT NULL DEFAULT 0
);
