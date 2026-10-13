package meuorm.core;

public enum OrmErrorKind {
    OTHER,             // qualquer erro não classificado
    CONNECTION,        // não conseguiu falar com o banco
    DUPLICATE,         // violou UNIQUE (ex.: e-mail repetido)
    FOREIGN_KEY,       // violou chave estrangeira
    NOT_NULL,          // campo obrigatório veio nulo
    TABLE_NOT_FOUND,
    COLUMN_NOT_FOUND,
    DEADLOCK,
    CONCURRENCY        // outro usuário alterou o registro antes (capítulo 14)
}
