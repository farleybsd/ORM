from typing import Any

import psycopg2

from meu_orm.core import dot_env
from meu_orm.core.i_orm import IOrm, Params, Row
from meu_orm.core.orm_exception import OrmErrorKind, OrmException


class Postgres(IOrm):
    def __init__(self) -> None:
        self._settings = {
            "host": dot_env.get("DB_HOST"),
            "port": dot_env.get("DB_PORT"),
            "user": dot_env.get("DB_USER"),
            "password": dot_env.get("DB_PASSWORD"),
            "dbname": dot_env.get("DB_NAME"),
            "options": "-c TimeZone=UTC",
        }
        self._connection: Any = None
        self._in_transaction = False

        # O "painel" da última operação
        self._db_error = ""
        self._last_sql = ""
        self._rows_affected = 0
        self._generated_id = 0
        self._data_table: list[Row] = []

    def _connect(self) -> None:
        self._connection = psycopg2.connect(**self._settings)

    def _disconnect(self) -> None:
        if self._connection is not None:
            self._connection.close()
            self._connection = None

    def exec_(self, sql: str, action: str, args: Params | None = None) -> None:
        # 1. Zera o painel: cada execução começa limpa
        self._last_sql = sql
        self._db_error = ""
        self._rows_affected = 0
        self._generated_id = 0
        self._data_table = []

        # 2. Sem transação manual aberta, este exec_ abre e fecha sua própria conexão
        own_connection = not self._in_transaction

        try:
            if own_connection:
                self._connect()

            # 3. Executa, com os valores separados do SQL
            with self._connection.cursor() as cursor:
                cursor.execute(sql, args)

                # 4. Escolhe o que ler do resultado
                if action == "query":
                    self._data_table = self._fetch_all(cursor)
                    self._rows_affected = len(self._data_table)
                elif action == "insert":
                    self._data_table = self._fetch_all(cursor)
                    self._rows_affected = len(self._data_table)
                    if self._data_table and "id" in self._data_table[0]:
                        self._generated_id = int(self._data_table[0]["id"])
                elif action in ("update", "delete", "exec"):
                    self._rows_affected = cursor.rowcount
                else:
                    raise ValueError(f"Ação inválida: '{action}'. "
                                     "Use query, insert, update, delete ou exec.")

            # 5. Na DB-API toda conexão já abre uma transação: sem commit, nada é gravado
            if own_connection:
                self._connection.commit()
        except psycopg2.Error as ex:
            self._db_error = str(ex).strip()
            raise OrmException(OrmErrorKind.OTHER, self._db_error) from ex
        finally:
            # 6. Fecha a conexão que ele mesmo abriu, dando certo ou errado
            if own_connection:
                self._disconnect()

    def _fetch_all(self, cursor: Any) -> list[Row]:
        if cursor.description is None:
            return []
        columns = [column[0] for column in cursor.description]
        return [dict(zip(columns, row)) for row in cursor.fetchall()]

    def get_status(self) -> str:
        return "success" if self._db_error == "" else "fail"

    def get_db_error(self) -> str:
        return self._db_error

    def get_rows_affected(self) -> int:
        return self._rows_affected

    def get_generated_id(self) -> int:
        return self._generated_id

    def get_data_table(self) -> list[Row]:
        return self._data_table

    def get_last_sql_sentence(self) -> str:
        return self._last_sql

    # Preenchidos no capítulo 8
    def select(self, table: str, fields: str = "*", where: str = "",
               args: Params | None = None, order: str = "") -> list[Row]:
        raise NotImplementedError

    def insert(self, table: str, data: Params) -> None:
        raise NotImplementedError

    def update(self, table: str, data: Params, where: str, args: Params | None = None) -> None:
        raise NotImplementedError

    def delete(self, table: str, where: str, args: Params | None = None) -> None:
        raise NotImplementedError

    # Preenchidos no capítulo 9
    def begin_transaction(self) -> None:
        raise NotImplementedError

    def commit(self) -> None:
        raise NotImplementedError

    def rollback(self) -> None:
        raise NotImplementedError

    def close(self) -> None:
        self._disconnect()
