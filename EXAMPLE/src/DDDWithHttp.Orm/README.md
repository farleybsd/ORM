# DDDWithHttp.Orm

Micro-ORM (class library) compartilhado pela `Apolices.Api` e pela `Sinistros.Api`, baseado no
`cap17-projeto-final` do curso (`orm-dotnet-main`), em versão assíncrona e para dois bancos.

## Bancos

| `DatabaseProvider` | Driver | Container |
|---|---|---|
| `Postgres` | Npgsql | `seguros-postgres` (porta 5432) |
| `SqlServer` | Microsoft.Data.SqlClient | `seguros-sqlserver` (porta 1433) |

Suba com `docker compose up -d` na pasta `container`. Cada API tem seu banco (`apolices`, `sinistros`)
nos dois servidores.

## Configuração (os dois bancos na mesma aplicação)

```json
"Orm": {
  "Connections": {
    "Postgres":  { "ConnectionString": "Host=...;Database=apolices;..." },
    "SqlServer": { "ConnectionString": "Server=...;Database=apolices;..." }
  }
},
"Persistencia": { "Banco": "Postgres" }
```

- `Orm:Connections` — todos os bancos disponíveis; a aplicação pode usar qualquer um ao mesmo tempo
  com `IDbSessionProvider.Get(DatabaseProvider.X)`.
- `Persistencia:Banco` — em qual banco o repositório da API grava (troque sem recompilar com
  `Persistencia__Banco=SqlServer`). Padrão: Apólices → Postgres, Sinistros → SQL Server.
- `GET /health` testa todos os bancos configurados.

## Transações e boas práticas

- **Unit of Work por requisição**: `IDbSessionProvider` (scoped) devolve a mesma sessão por banco
  durante a requisição; no fim, transação esquecida aberta sofre **rollback** automático.
- **`await using var tx = await session.BeginTransactionAsync()`**: sem `CommitAsync`, o dispose faz rollback.
- **`ExecuteInTransactionAsync`**: commit no sucesso, rollback na exceção; se já houver transação
  aberta, participa dela (o caso de uso pode ampliar a fronteira); repete a transação inteira em
  **deadlock/serialização** (`MaxTransactionAttempts`), nunca em falha de conexão.
- **Concorrência otimista** (`UpdateVersionedAsync` / `DeleteVersionedAsync`): `WHERE versao = @v`
  e `versao = versao + 1`; 0 linhas → `OrmErrorKind.Concurrency` (as APIs devolvem 409).
- **Ordem de lock fixa**: os repositórios tocam sempre a raiz do agregado primeiro, evitando deadlock.
- **Conexão**: aberta tarde e devolvida ao pool logo após cada comando; só fica presa durante transação.
- **Segurança**: valores sempre como parâmetro; nomes de tabela/coluna validados; `UPDATE`/`DELETE`
  sem `WHERE` bloqueados; log só do SQL (nunca dos valores).
- **Erros traduzidos**: `OrmException.Kind` é igual para os dois bancos (`Duplicate`, `ForeignKey`,
  `CheckViolation`, `Deadlock`, `Timeout`...).
- **Sem transação distribuída**: Postgres e SQL Server confirmam separadamente. Para consistência
  entre os dois use outbox/eventos.
- **Convenções**: enum gravado como texto; `DateTime` sempre UTC; `DateOnly` ↔ `DATE`; ids `Guid`
  gerados pela aplicação.
