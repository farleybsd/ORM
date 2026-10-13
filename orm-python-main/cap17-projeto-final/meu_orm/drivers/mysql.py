from typing import Any

import pymysql

from meu_orm.core import dot_env
from meu_orm.core.orm_exception import OrmErrorKind, OrmException
from meu_orm.drivers.driver_base import DriverBase


class MySql(DriverBase):
    def __init__(self) -> None:
        super().__init__()
        self._settings = {
            "host": dot_env.get("DB_HOST"),
            "port": int(dot_env.get("DB_PORT")),
            "user": dot_env.get("DB_USER"),
            "password": dot_env.get("DB_PASSWORD"),
            "database": dot_env.get("DB_NAME"),
            "charset": "utf8mb4",
        }

    def _create_connection(self) -> Any:
        return pymysql.connect(**self._settings)

    def _error_class(self) -> type[Exception]:
        return pymysql.Error

    # MySQL não tem RETURNING
    def _insert_sql(self, table: str, fields: str, values: str) -> str:
        return f"INSERT INTO {table} ({fields}) VALUES ({values})"

    # ...então o id vem de uma propriedade do próprio cursor
    def _read_insert_result(self, cursor: Any) -> None:
        self._rows_affected = cursor.rowcount
        self._generated_id = cursor.lastrowid

    def _translate_error(self, ex: Exception) -> OrmException:
        code = ex.args[0] if ex.args and isinstance(ex.args[0], int) else 0
        text = str(ex.args[1]) if len(ex.args) > 1 else str(ex)

        if code == 1062:
            return OrmException(OrmErrorKind.DUPLICATE, "Registro duplicado")
        if code in (1451, 1452):
            return OrmException(OrmErrorKind.FOREIGN_KEY, "Referência inválida")
        if code == 1048:
            return OrmException(OrmErrorKind.NOT_NULL, text)
        if code == 1146:
            return OrmException(OrmErrorKind.TABLE_NOT_FOUND, text)
        if code == 1054:
            return OrmException(OrmErrorKind.COLUMN_NOT_FOUND, text)
        if code == 1213:
            return OrmException(OrmErrorKind.DEADLOCK, "Deadlock detectado")
        if code in (1045, 2003):
            return OrmException(OrmErrorKind.CONNECTION, text)
        return OrmException(OrmErrorKind.OTHER, text)
