from meu_orm.core import dot_env
from meu_orm.core.orm import Orm

dot_env.load()

with Orm() as orm:
    orm.exec_("SELECT id, nome, email FROM cliente", "query")

    for row in orm.get_data_table():
        print(f"{row['id']} - {row['nome']}")

    print(f"{orm.get_rows_affected()} linha(s). SQL: {orm.get_last_sql_sentence()}")
