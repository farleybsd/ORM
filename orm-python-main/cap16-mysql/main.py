import uuid
from decimal import Decimal

from meu_orm.core import dot_env
from meu_orm.core.orm import Orm
from meu_orm.models.cliente import Cliente
from meu_orm.models.fornecedor import Fornecedor

dot_env.load()

# Um cliente com um pedido, para o campo calculado ter o que contar
cliente = Cliente(nome="Maria", email=f"maria-{uuid.uuid4().hex}@email.com")
cliente.save()
with Orm() as orm:
    orm.insert("pedido", {"cliente_id": cliente.id, "total": Decimal("50.00")})

c = Cliente()
c.load_query("""
    SELECT c.*,
           (SELECT COUNT(*) FROM pedido p WHERE p.cliente_id = c.id) AS total_pedidos
      FROM cliente c
     WHERE c.id = %(id)s""", {"id": cliente.id})

print(f"{c.nome} tem {c.total_pedidos} pedido(s)")

c.nome = "Novo nome"
c.save()   # UPDATE só com nome e updated_at; total_pedidos nem aparece
print(f"Salvo: {c.nome}")

# @table: a classe Fornecedor grava na tabela tb_fornecedor_legado
f = Fornecedor(razao_social="ACME Ltda")
f.save()
print(f"Fornecedor {f.id} gravado em tb_fornecedor_legado")
