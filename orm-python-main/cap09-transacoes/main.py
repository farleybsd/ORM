import uuid
from decimal import Decimal

from meu_orm.core import dot_env
from meu_orm.core.orm import Orm
from meu_orm.core.orm_exception import OrmException

dot_env.load()


def gravar_pedido(orm: Orm, cliente_id: int, segundo_produto: str | None) -> None:
    orm.begin_transaction()
    try:
        orm.insert("pedido", {"cliente_id": cliente_id, "total": Decimal("300.00")})
        pedido_id = orm.get_generated_id()

        orm.insert("pedido_item", {"pedido_id": pedido_id, "produto": "Teclado", "valor": Decimal("100.00")})
        orm.insert("pedido_item", {"pedido_id": pedido_id, "produto": segundo_produto, "valor": Decimal("200.00")})

        orm.commit()
        print(f"Pedido {pedido_id} gravado.")
    except OrmException as ex:
        orm.rollback()
        print(f"Nada foi gravado: {ex}")


with Orm() as orm:
    # Um cliente para ser dono dos pedidos
    orm.insert("cliente", {"nome": "Cliente do pedido", "email": f"pedido-{uuid.uuid4().hex}@email.com"})
    cliente_id = orm.get_generated_id()

    gravar_pedido(orm, cliente_id, "Monitor")   # tudo certo: grava
    gravar_pedido(orm, cliente_id, None)        # teste do rollback: nada é gravado
