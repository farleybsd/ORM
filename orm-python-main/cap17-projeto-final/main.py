import time

from meu_orm.core import dot_env
from meu_orm.core.orm import Orm
from meu_orm.core.orm_exception import OrmErrorKind, OrmException
from meu_orm.models.cliente import Cliente

dot_env.load()
print(f"Driver: {dot_env.get('ORM_DRIVER')}\n")

# 1. Criar
maria = Cliente(nome="Maria", email=f"maria{time.time_ns()}@email.com")
maria.save()
print(f"1. Criada: id {maria.id}")

# 2. Duplicado: o banco recusa e o ORM traduz
try:
    Cliente(nome="Outra Maria", email=maria.email).save()
except OrmException as ex:
    if ex.kind is not OrmErrorKind.DUPLICATE:
        raise
    print("2. E-mail repetido recusado, como esperado")

# 3. Ler e alterar só um campo (dirty-check)
c = Cliente()
c.load(maria.id)
c.nome = "Maria Souza"
c.save()
print(f"3. Alterada: {c.nome}")

# 4. Save sem mudança não vai ao banco
print(f"4. Gravou de novo? {c.save()}")

# 5. Conflito: dois usuários editando ao mesmo tempo
ana = Cliente()
ana.load(maria.id)
bruno = Cliente()
bruno.load(maria.id)

ana.ativo = False
ana.save()

bruno.nome = "Nome do Bruno"
try:
    bruno.save()
except OrmException as ex:
    if ex.kind is not OrmErrorKind.CONCURRENCY:
        raise
    print("5. Conflito detectado: Bruno precisa recarregar")

# 6. Listar com SQL visível
with Orm() as orm:
    rows = orm.select("cliente", "id, nome, ativo", "", None, "id DESC")
    print(f"6. {len(rows)} cliente(s). SQL: {orm.get_last_sql_sentence()}")

    for row in rows[:5]:
        print(f"   {row['id']:>4} | {row['nome']:<20} | ativo: {bool(row['ativo'])}")

# 7. Apagar
c.load(maria.id)
c.delete()
print(f"7. Apagada. Ainda existe? {Cliente().load(maria.id)}")
