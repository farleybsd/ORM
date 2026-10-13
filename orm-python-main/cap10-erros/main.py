import uuid
from decimal import Decimal

from meu_orm.core import dot_env
from meu_orm.core.orm import Orm
from meu_orm.core.orm_exception import OrmErrorKind, OrmException

dot_env.load()


def testar(titulo: str, acao) -> None:
    try:
        acao()
    except OrmException as ex:
        print(f"{titulo}: kind={ex.kind.name} | {ex}")


with Orm() as orm:
    email = f"ana-{uuid.uuid4().hex}@email.com"
    orm.insert("cliente", {"nome": "Ana", "email": email})

    try:
        orm.insert("cliente", {"nome": "Ana de novo", "email": email})
    except OrmException as ex:
        if ex.kind is OrmErrorKind.DUPLICATE:
            print("Este e-mail já está cadastrado.")
        elif ex.kind is OrmErrorKind.CONNECTION:
            print("Banco fora do ar. Tente em alguns minutos.")
        else:
            raise

    # Os outros tipos de erro, um de cada
    testar("Chave estrangeira", lambda: orm.insert("pedido", {"cliente_id": -1, "total": Decimal("1")}))
    testar("Campo obrigatório", lambda: orm.insert("cliente", {"nome": None, "email": "x@x.com"}))
    testar("Tabela inexistente", lambda: orm.exec_("SELECT * FROM nao_existe", "query"))
    testar("Coluna inexistente", lambda: orm.exec_("SELECT coluna_x FROM cliente", "query"))
