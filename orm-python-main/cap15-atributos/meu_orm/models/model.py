import time
import typing
from dataclasses import dataclass, fields
from decimal import Decimal
from typing import Any

from meu_orm.core.orm import Orm
from meu_orm.core.orm_exception import OrmErrorKind, OrmException
from meu_orm.models.naming import to_snake

SIMPLE_TYPES = (bool, int, float, str, Decimal)


@dataclass
class Model:
    id: int = 0
    inserted_at: int = 0
    updated_at: int = 0

    def __post_init__(self) -> None:
        self._snapshot: dict[str, Any] = {}

    @classmethod
    def table_name(cls) -> str:
        explicit = getattr(cls, "__table__", None)
        if explicit:
            return explicit

        name = cls.__name__.removesuffix("Model")
        return to_snake(name)

    def load(self, id: int) -> bool:
        with Orm() as orm:
            rows = orm.select(self.table_name(), "*", "id = %(id)s", {"id": id})

        if not rows:
            return False

        self._fill(rows[0])
        self._take_snapshot()
        return True

    def load_query(self, sql: str, args: dict[str, Any] | None = None) -> bool:
        with Orm() as orm:
            orm.exec_(sql, "query", args)
            rows = orm.get_data_table()

        if not rows:
            return False

        self._fill(rows[0])
        self._take_snapshot()
        return True

    def save(self) -> bool:
        data: dict[str, Any] = {}

        for f in fields(self):
            if f.name in ("id", "inserted_at", "updated_at"):
                continue

            if f.metadata.get("calc"):
                continue

            value = getattr(self, f.name)

            if self.id != 0 and f.name in self._snapshot and self._snapshot[f.name] == value:
                continue

            data[f.name] = value

        if self.id != 0 and not data:
            return False

        now = max(int(time.time() * 1000), self.updated_at + 1)

        with Orm() as orm:
            if self.id == 0:
                data["inserted_at"] = now
                data["updated_at"] = now
                orm.insert(self.table_name(), data)

                self.id = orm.get_generated_id()
                self.inserted_at = now
                self.updated_at = now
            else:
                data["updated_at"] = now

                orm.update(self.table_name(), data,
                           "id = %(id)s AND updated_at = %(old_updated_at)s",
                           {"id": self.id, "old_updated_at": self.updated_at})

                if orm.get_rows_affected() == 0:
                    raise OrmException(OrmErrorKind.CONCURRENCY,
                                       "Este registro foi alterado ou excluído por outro usuário. "
                                       "Recarregue os dados e tente novamente.")

                self.updated_at = now

        self._take_snapshot()
        return True

    def delete(self) -> None:
        if self.id == 0:
            raise ValueError("Não dá para apagar um registro que nunca foi gravado.")

        with Orm() as orm:
            orm.delete(self.table_name(), "id = %(id)s", {"id": self.id})

        self.id = 0
        self._snapshot.clear()

    def _fill(self, row: dict[str, Any]) -> None:
        hints = typing.get_type_hints(type(self))

        for f in fields(self):
            if f.name not in row:
                continue

            value = row[f.name]
            target = hints.get(f.name)

            if value is not None and target in SIMPLE_TYPES and not isinstance(value, target):
                value = target(value)

            setattr(self, f.name, value)

    def _take_snapshot(self) -> None:
        self._snapshot = {f.name: getattr(self, f.name) for f in fields(self)}
