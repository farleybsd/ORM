from typing import Any

import psycopg2
from psycopg2 import errorcodes

from meu_orm.core import dot_env
from meu_orm.core.orm_exception import OrmErrorKind, OrmException
from meu_orm.drivers.driver_base import DriverBase


class Postgres(DriverBase):
    def __init__(self) -> None:
        super().__init__()
        self._settings = {
            "host": dot_env.get("DB_HOST"),
            "port": dot_env.get("DB_PORT"),
            "user": dot_env.get("DB_USER"),
            "password": dot_env.get("DB_PASSWORD"),
            "dbname": dot_env.get("DB_NAME"),
            "options": "-c TimeZone=UTC",
        }

    def _create_connection(self) -> Any:
        return psycopg2.connect(**self._settings)

    def _error_class(self) -> type[Exception]:
        return psycopg2.Error

    def _insert_sql(self, table: str, fields: str, values: str) -> str:
        return f"INSERT INTO {table} ({fields}) VALUES ({values}) RETURNING *"

    def _read_insert_result(self, cursor: Any) -> None:
        self._data_table = self._fetch_all(cursor)
        self._rows_affected = len(self._data_table)

        if self._data_table and "id" in self._data_table[0]:
            self._generated_id = int(self._data_table[0]["id"])

    def _translate_error(self, ex: Exception) -> OrmException:
        code = getattr(ex, "pgcode", None)
        diag = getattr(ex, "diag", None)

        if code is None:
            return OrmException(OrmErrorKind.CONNECTION, f"Falha de conexão com o banco: {ex}".strip())

        constraint = (diag.constraint_name if diag else None) or ""
        column = (diag.column_name if diag else None) or ""
        text = (diag.message_primary if diag else None) or str(ex)

        if code == errorcodes.UNIQUE_VIOLATION:
            return OrmException(OrmErrorKind.DUPLICATE, f"Registro duplicado ({constraint})", constraint)
        if code == errorcodes.FOREIGN_KEY_VIOLATION:
            return OrmException(OrmErrorKind.FOREIGN_KEY, f"Referência inválida ({constraint})", constraint)
        if code == errorcodes.NOT_NULL_VIOLATION:
            return OrmException(OrmErrorKind.NOT_NULL, f"Campo obrigatório não informado: {column}", column)
        if code == errorcodes.UNDEFINED_TABLE:
            return OrmException(OrmErrorKind.TABLE_NOT_FOUND, text)
        if code == errorcodes.UNDEFINED_COLUMN:
            return OrmException(OrmErrorKind.COLUMN_NOT_FOUND, text)
        if code == errorcodes.DEADLOCK_DETECTED:
            return OrmException(OrmErrorKind.DEADLOCK, "Deadlock detectado. Tente a operação novamente.")
        return OrmException(OrmErrorKind.OTHER, text)
