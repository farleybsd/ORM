from meu_orm.core import dot_env
from meu_orm.core.orm import Orm

dot_env.load()

# Ainda não existe nenhum driver. Rodando agora, a fachada procura
# meu_orm.drivers.postgres e explica o que está faltando.
try:
    with Orm() as orm:
        pass
except RuntimeError as ex:
    print(ex)
