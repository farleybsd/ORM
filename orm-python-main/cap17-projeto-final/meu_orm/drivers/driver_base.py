from abc import abstractmethod
from typing import Any

from meu_orm.core.i_orm import IOrm, Params, Row
from meu_orm.core.orm_exception import OrmException


class DriverBase(IOrm):
    def __init__(self) -> None:
        self._connection: Any = None
        self._in_transaction = False

        self._db_error = ""
        self._last_sql = ""
        self._rows_affected = 0
        self._generated_id = 0
        self._data_table: list[Row] = []

    # ===== Os buracos que cada banco preenche =====
    @abstractmethod
    def _create_connection(self) -> Any: ...

    @abstractmethod
    def _error_class(self) -> type[Exception]: ...

    @abstractmethod
    def _insert_sql(self, table: str, fields: str, values: str) -> str: ...

    @abstractmethod
    def _read_insert_result(self, cursor: Any) -> None: ...

    @abstractmethod
    def _translate_error(self, ex: Exception) -> OrmException: ...

    # ===== O roteiro comum =====
    def exec_(self, sql: str, action: str, args: Params | None = None) -> None:
        self._last_sql = sql
        self._db_error = ""
        self._rows_affected = 0
        self._generated_id = 0
        self._data_table = []

        own_connection = not self._in_transaction

        try:
            if own_connection:
                self._connect()

            with self._connection.cursor() as cursor:
                cursor.execute(sql, args)

                if action == "query":
                    self._data_table = self._fetch_all(cursor)
                    self._rows_affected = len(self._data_table)
                elif action == "insert":
                    self._read_insert_result(cursor)   # cada banco pega o id do seu jeito
                elif action in ("update", "delete", "exec"):
                    self._rows_affected = cursor.rowcount
                else:
                    raise ValueError(f"Ação inválida: '{action}'.")

            if own_connection:
                self._connection.commit()
        except self._error_class() as ex:
            error = self._translate_error(ex)   # cada banco traduz seus códigos
            self._db_error = str(error)
            raise error from ex
        finally:
            if own_connection:
                self._disconnect()

    def _fetch_all(self, cursor: Any) -> list[Row]:
        if cursor.description is None:
            return []
        columns = [column[0] for column in cursor.description]
        return [dict(zip(columns, row)) for row in cursor.fetchall()]

    def select(self, table: str, fields: str = "*", where: str = "",
               args: Params | None = None, order: str = "") -> list[Row]:
        sql = f"SELECT {fields} FROM {table}"
        if where != "":
            sql += f" WHERE {where}"
        if order != "":
            sql += f" ORDER BY {order}"

        self.exec_(sql, "query", args)
        return self._data_table

    def insert(self, table: str, data: Params) -> None:
        fields = ", ".join(data)
        values = ", ".join(f"%({key})s" for key in data)
        self.exec_(self._insert_sql(table, fields, values), "insert", data)

    def update(self, table: str, data: Params, where: str, args: Params | None = None) -> None:
        if not where.strip():
            raise ValueError("Update sem WHERE bloqueado.")

        parameters = dict(args or {})
        sets = []
        for column, value in data.items():
            sets.append(f"{column} = %(v_{column})s")
            parameters[f"v_{column}"] = value

        self.exec_(f"UPDATE {table} SET {', '.join(sets)} WHERE {where}", "update", parameters)

    def delete(self, table: str, where: str, args: Params | None = None) -> None:
        if not where.strip():
            raise ValueError("Delete sem WHERE bloqueado.")

        self.exec_(f"DELETE FROM {table} WHERE {where}", "delete", args)

    def begin_transaction(self) -> None:
        if self._in_transaction:
            raise RuntimeError("Já existe uma transação aberta.")

        self._connect()
        self._in_transaction = True

    def commit(self) -> None:
        if not self._in_transaction:
            raise RuntimeError("Nenhuma transação aberta para confirmar.")

        self._connection.commit()
        self._end_transaction()

    def rollback(self) -> None:
        if not self._in_transaction:
            return

        self._connection.rollback()
        self._end_transaction()

    def _end_transaction(self) -> None:
        self._in_transaction = False
        self._disconnect()

    def _connect(self) -> None:
        self._connection = self._create_connection()

    def _disconnect(self) -> None:
        if self._connection is not None:
            self._connection.close()
            self._connection = None

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

    def close(self) -> None:
        try:
            self.rollback()
        except Exception:
            pass  # conexão já caída: o banco desfaz sozinho
        self._in_transaction = False
        self._disconnect()
