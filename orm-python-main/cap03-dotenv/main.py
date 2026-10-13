from meu_orm.core import dot_env

dot_env.load()
print(f"Driver: {dot_env.get('ORM_DRIVER')}")
print(f"Banco: {dot_env.get('DB_NAME')}")
