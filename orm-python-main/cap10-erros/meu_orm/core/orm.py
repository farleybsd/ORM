import importlib

from meu_orm.core import dot_env
from meu_orm.core.i_orm import IOrm, Params, Row


class Orm(IOrm):
    def __init__(self) -> None:
        name = dot_env.get("ORM_DRIVER")                 # ex.: "Postgres"
        module_name = f"meu_orm.drivers.{name.lower()}"  # ex.: meu_orm.drivers.postgres

        try:
            module = importlib.import_module(module_name)
            driver_class = getattr(module, name)
        except ModuleNotFoundError as ex:
            if ex.name is None or not module_name.startswith(ex.name):
                raise  # faltou outra coisa (ex.: o pacote psycopg2), não o driver
            raise RuntimeError(
                f"Driver '{name}' não encontrado. Confira ORM_DRIVER no .env "
                "(maiúsculas e minúsculas fazem diferença: Postgres, não postgres).") from None
        except AttributeError:
            raise RuntimeError(
                f"O arquivo {module_name} existe, mas não tem a classe '{name}'.") from None

        if not (isinstance(driver_class, type) and issubclass(driver_class, IOrm)):
            raise RuntimeError(f"A classe '{name}' existe, mas não implementa IOrm.")

        self._driver: IOrm = driver_class()

    # Daqui para baixo: cada método só repassa para o driver
    def exec_(self, sql: str, action: str, args: Params | None = None) -> None:
        self._driver.exec_(sql, action, args)

    def select(self, table: str, fields: str = "*", where: str = "",
               args: Params | None = None, order: str = "") -> list[Row]:
        return self._driver.select(table, fields, where, args, order)

    def insert(self, table: str, data: Params) -> None:
        self._driver.insert(table, data)

    def update(self, table: str, data: Params, where: str, args: Params | None = None) -> None:
        self._driver.update(table, data, where, args)

    def delete(self, table: str, where: str, args: Params | None = None) -> None:
        self._driver.delete(table, where, args)

    def begin_transaction(self) -> None:
        self._driver.begin_transaction()

    def commit(self) -> None:
        self._driver.commit()

    def rollback(self) -> None:
        self._driver.rollback()

    def get_status(self) -> str:
        return self._driver.get_status()

    def get_db_error(self) -> str:
        return self._driver.get_db_error()

    def get_rows_affected(self) -> int:
        return self._driver.get_rows_affected()

    def get_generated_id(self) -> int:
        return self._driver.get_generated_id()

    def get_data_table(self) -> list[Row]:
        return self._driver.get_data_table()

    def get_last_sql_sentence(self) -> str:
        return self._driver.get_last_sql_sentence()

    def close(self) -> None:
        self._driver.close()
