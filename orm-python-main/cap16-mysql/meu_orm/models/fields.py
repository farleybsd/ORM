from dataclasses import field
from typing import Any


def calc_field(default: Any = None) -> Any:
    """Campo calculado: é preenchido em consultas, mas nunca vai para INSERT/UPDATE."""
    return field(default=default, metadata={"calc": True})


def table(name: str):
    """Nome de tabela explícito, para quando a convenção não serve."""
    def decorate(cls):
        cls.__table__ = name
        return cls
    return decorate
