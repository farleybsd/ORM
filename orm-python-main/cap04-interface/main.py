from abc import ABC, abstractmethod


class ITomada(ABC):
    @abstractmethod
    def voltagem(self) -> int: ...


class Hidreletrica(ITomada):
    def voltagem(self) -> int:
        return 127


class Esquecida(ITomada):
    pass   # não implementou voltagem()


tomada = Hidreletrica()
print(f"Voltagem: {tomada.voltagem()} V")

try:
    Esquecida()
except TypeError as ex:
    print(f"Python recusou: {ex}")

# O contrato do ORM também carrega sem erro:
from meu_orm.core.i_orm import IOrm
print(f"Métodos que todo driver precisa ter: {len(IOrm.__abstractmethods__)}")
