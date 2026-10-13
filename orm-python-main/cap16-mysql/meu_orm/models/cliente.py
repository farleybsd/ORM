from dataclasses import dataclass

from meu_orm.models.fields import calc_field
from meu_orm.models.model import Model


@dataclass
class Cliente(Model):
    nome: str = ""
    email: str = ""
    ativo: bool = True
    total_pedidos: int = calc_field(0)   # não existe coluna total_pedidos
