from dataclasses import dataclass

from meu_orm.models.model import Model


@dataclass
class Cliente(Model):
    nome: str = ""
    email: str = ""
    ativo: bool = True
