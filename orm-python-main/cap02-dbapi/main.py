import uuid

import psycopg2

connection = psycopg2.connect(host="localhost", port=5432, user="postgres",
                              password="postgres", dbname="loja")

with connection.cursor() as cursor:
    # No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
    cursor.execute(
        "INSERT INTO cliente (nome, email) VALUES (%(nome)s, %(email)s) RETURNING id",
        {"nome": "Maria", "email": f"maria-{uuid.uuid4().hex}@email.com"})
    novo_id = cursor.fetchone()[0]

connection.commit()   # sem commit, o INSERT é desfeito ao fechar a conexão
print(f"Cliente criado com id {novo_id}")

# Um SELECT na mão
with connection.cursor() as cursor:
    cursor.execute("SELECT id, nome, email FROM cliente")
    for row in cursor.fetchall():
        print(f"{row[0]} - {row[1]} - {row[2]}")

connection.close()
