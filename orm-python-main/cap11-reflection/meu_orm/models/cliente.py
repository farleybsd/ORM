from dataclasses import dataclass


@dataclass
class Cliente:
    id: int = 0
    nome: str = ""
    email: str = ""
    ativo: bool = True
