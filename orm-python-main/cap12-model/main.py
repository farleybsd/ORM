import uuid

from meu_orm.core import dot_env
from meu_orm.models.cliente import Cliente

dot_env.load()

# No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
novo = Cliente(nome="Carla", email=f"carla-{uuid.uuid4().hex}@email.com")
novo.save()                                      # INSERT
print(f"Criada com id {novo.id}")

carla = Cliente()
if carla.load(novo.id):
    carla.nome = "Carla Souza"
    carla.save()                                 # UPDATE (ainda grava todas as colunas)
    print(f"Alterada: {carla.nome}")
