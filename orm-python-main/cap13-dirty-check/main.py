import uuid

from meu_orm.core import dot_env
from meu_orm.models.cliente import Cliente

dot_env.load()

nova = Cliente(nome="Maria", email=f"maria-{uuid.uuid4().hex}@email.com")
nova.save()

c = Cliente()
c.load(nova.id)

c.nome = "Maria Souza"
print(f"Gravou? {c.save()}")          # True: só nome e updated_at vão no UPDATE

gravou = c.save()
print(f"Gravou de novo? {gravou}")    # False: nada mudou desde o último save
