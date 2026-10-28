# Leia-me

## Banco de dados PostgreSQL (Docker)

Comando para criar o container do PostgreSQL com o banco `loja`:

```bash
docker run -d --name postgres-loja -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=loja -p 5432:5432 -v postgres-loja-data:/var/lib/postgresql/data postgres:17
```

Comandos úteis:

```bash
docker start postgres-loja   # iniciar o container
docker stop postgres-loja    # parar o container
docker exec -it postgres-loja psql -U postgres -d loja   # acessar o banco via psql
```

String de conexão (já configurada em `appsettings.json`):

```
Host=localhost;Port=5432;Database=loja;Username=postgres;Password=postgres
```
