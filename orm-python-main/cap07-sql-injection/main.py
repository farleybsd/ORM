from meu_orm.core import dot_env
from meu_orm.core.orm import Orm

dot_env.load()

with Orm() as orm:
    # O que o usuário digitou no campo de e-mail:
    email = "' OR '1'='1"

    # ERRADO: o valor colado no SQL vira comando
    orm.exec_("SELECT * FROM cliente WHERE email = '" + email + "'", "query")
    print(f"Concatenado: {orm.get_rows_affected()} linha(s) (devolveu TODOS os clientes)")

    # CERTO: o valor viaja como parâmetro
    orm.exec_("SELECT * FROM cliente WHERE email = %(email)s", "query", {"email": email})
    print(f"Com parâmetro: {orm.get_rows_affected()} linha(s)")

    # LIKE: o % vai dentro do valor
    texto = "ari"
    orm.exec_("SELECT * FROM cliente WHERE nome ILIKE %(busca)s", "query", {"busca": f"%{texto}%"})
    print(f"LIKE '%{texto}%': {orm.get_rows_affected()} linha(s)")

    # Ordenação escolhida pelo usuário: compare com uma lista fixa
    escolha_do_usuario = "email; DROP TABLE cliente"
    permitidas = ["nome", "email", "inserted_at"]
    order = escolha_do_usuario if escolha_do_usuario in permitidas else "nome"
    print(f"Ordenação usada: {order}")
