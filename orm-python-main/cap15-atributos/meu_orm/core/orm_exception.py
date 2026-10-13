from enum import Enum


class OrmErrorKind(Enum):
    OTHER = "other"                    # qualquer erro não classificado
    CONNECTION = "connection"          # não conseguiu falar com o banco
    DUPLICATE = "duplicate"            # violou UNIQUE (ex.: e-mail repetido)
    FOREIGN_KEY = "foreign_key"        # violou chave estrangeira
    NOT_NULL = "not_null"              # campo obrigatório veio nulo
    TABLE_NOT_FOUND = "table_not_found"
    COLUMN_NOT_FOUND = "column_not_found"
    DEADLOCK = "deadlock"
    CONCURRENCY = "concurrency"        # outro usuário alterou o registro antes (capítulo 14)


class OrmException(Exception):
    def __init__(self, kind: OrmErrorKind, message: str, detail: str = "") -> None:
        super().__init__(message)
        self.kind = kind
        self.detail = detail   # nome da constraint ou da coluna, quando houver
