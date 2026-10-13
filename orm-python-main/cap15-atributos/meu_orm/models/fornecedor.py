from dataclasses import dataclass

from meu_orm.models.fields import table
from meu_orm.models.model import Model


@table("tb_fornecedor_legado")
@dataclass
class Fornecedor(Model):
    razao_social: str = ""
