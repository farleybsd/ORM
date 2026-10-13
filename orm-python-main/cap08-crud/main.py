import uuid

from meu_orm.core import dot_env
from meu_orm.core.orm import Orm

dot_env.load()

with Orm() as orm:
    # No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
    orm.insert("cliente", {"nome": "João", "email": f"joao-{uuid.uuid4().hex}@email.com"})
    novo_id = orm.get_generated_id()
    print(f"Novo id: {novo_id}")

    orm.update("cliente", {"nome": "João Pedro"}, "id = %(id)s", {"id": novo_id})
    print(f"{orm.get_rows_affected()} linha(s) alterada(s)")

    rows = orm.select("cliente", "nome", "id = %(id)s", {"id": novo_id})
    print(rows[0]["nome"])   # João Pedro

    orm.delete("cliente", "id = %(id)s", {"id": novo_id})
    print(orm.get_last_sql_sentence())   # DELETE FROM cliente WHERE id = %(id)s

    # A trava do WHERE
    try:
        orm.update("cliente", {"nome": "Todo mundo"}, "")
    except ValueError as ex:
        print(ex)
