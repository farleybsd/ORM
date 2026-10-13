import uuid

from meu_orm.core import dot_env
from meu_orm.core.orm_exception import OrmErrorKind, OrmException
from meu_orm.models.cliente import Cliente

dot_env.load()

registro = Cliente(nome="Cliente", email=f"conflito-{uuid.uuid4().hex}@email.com")
registro.save()

ana = Cliente()
ana.load(registro.id)

bruno = Cliente()
bruno.load(registro.id)        # os dois leram a mesma versão

ana.nome = "Nome da Ana"
ana.save()                     # grava e muda o updated_at
print("Ana gravou.")

bruno.nome = "Nome do Bruno"
try:
    bruno.save()               # condição falha: zero linhas
except OrmException as ex:
    if ex.kind is not OrmErrorKind.CONCURRENCY:
        raise
    print(f"Bruno: {ex}")
