import typing
from dataclasses import fields

from meu_orm.models.cliente import Cliente
from meu_orm.models.naming import to_snake

cliente = Cliente(id=7, nome="Maria", email="maria@email.com")

# 1. Descobrir os campos
hints = typing.get_type_hints(Cliente)        # nome do campo -> tipo declarado

for f in fields(cliente):
    # 2. Ler o valor pelo nome
    value = getattr(cliente, f.name)
    print(f"{f.name} ({hints[f.name].__name__}) = {value}")

# 3. Alterar um valor pelo nome
setattr(cliente, "nome", "Maria Silva")
print(cliente.nome)                             # Maria Silva

for nome in ["Cliente", "PedidoItem", "Fornecedor"]:
    print(f"{nome} -> {to_snake(nome)}")
