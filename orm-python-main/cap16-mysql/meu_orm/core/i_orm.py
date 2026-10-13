from abc import ABC, abstractmethod
from typing import Any

Row = dict[str, Any]      # uma linha do resultado: coluna -> valor
Params = dict[str, Any]   # parâmetros: nome -> valor


class IOrm(ABC):
    # A porta única: todo SQL passa por aqui
    @abstractmethod
    def exec_(self, sql: str, action: str, args: Params | None = None) -> None: ...

    # Atalhos de CRUD (montam o SQL e chamam o exec_)
    @abstractmethod
    def select(self, table: str, fields: str = "*", where: str = "",
               args: Params | None = None, order: str = "") -> list[Row]: ...

    @abstractmethod
    def insert(self, table: str, data: Params) -> None: ...

    @abstractmethod
    def update(self, table: str, data: Params, where: str, args: Params | None = None) -> None: ...

    @abstractmethod
    def delete(self, table: str, where: str, args: Params | None = None) -> None: ...

    # Transação manual
    @abstractmethod
    def begin_transaction(self) -> None: ...

    @abstractmethod
    def commit(self) -> None: ...

    @abstractmethod
    def rollback(self) -> None: ...

    # Estado da última operação
    @abstractmethod
    def get_status(self) -> str: ...

    @abstractmethod
    def get_db_error(self) -> str: ...

    @abstractmethod
    def get_rows_affected(self) -> int: ...

    @abstractmethod
    def get_generated_id(self) -> int: ...

    @abstractmethod
    def get_data_table(self) -> list[Row]: ...

    @abstractmethod
    def get_last_sql_sentence(self) -> str: ...

    # Libera a conexão e desfaz transação esquecida
    @abstractmethod
    def close(self) -> None: ...

    # Permite usar: with Orm() as orm:
    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()
