import typing
import time
from dataclasses import dataclass, fields
from decimal import Decimal
from typing import Any

from meu_orm.core.orm import Orm
from meu_orm.models.naming import to_snake

SIMPLE_TYPES = (bool, int, float, str, Decimal)


@dataclass
class Model:
    # Colunas que toda tabela do sistema tem
    id: int = 0
    inserted_at: int = 0
    updated_at: int = 0

    @classmethod
    def table_name(cls) -> str:
        # Nome da tabela pela convenção: Cliente -> cliente, PedidoItemModel -> pedido_item
        name = cls.__name__.removesuffix("Model")
        return to_snake(name)

    def load(self, id: int) -> bool:
        with Orm() as orm:
            rows = orm.select(self.table_name(), "*", "id = %(id)s", {"id": id})

        if not rows:
            return False   # não achou: o objeto continua como estava

        self._fill(rows[0])
        return True

    def save(self) -> None:
        # Monta o dicionário coluna -> valor a partir dos campos da dataclass
        data: dict[str, Any] = {}

        for f in fields(self):
            if f.name in ("id", "inserted_at", "updated_at"):
                continue   # controlados pelo próprio Model

            data[f.name] = getattr(self, f.name)

        now = int(time.time() * 1000)

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
                orm.update(self.table_name(), data, "id = %(id)s", {"id": self.id})
                self.updated_at = now

    def delete(self) -> None:
        if self.id == 0:
            raise ValueError("Não dá para apagar um registro que nunca foi gravado.")

        with Orm() as orm:
            orm.delete(self.table_name(), "id = %(id)s", {"id": self.id})

        self.id = 0

    # Copia uma linha do banco para os campos do objeto
    def _fill(self, row: dict[str, Any]) -> None:
        hints = typing.get_type_hints(type(self))   # nome do campo -> tipo declarado

        for f in fields(self):
            if f.name not in row:
                continue   # campo sem coluna correspondente

            value = row[f.name]
            target = hints.get(f.name)

            # O MySQL devolve BOOLEAN como 0/1: converte para o tipo declarado
            if value is not None and target in SIMPLE_TYPES and not isinstance(value, target):
                value = target(value)

            setattr(self, f.name, value)
