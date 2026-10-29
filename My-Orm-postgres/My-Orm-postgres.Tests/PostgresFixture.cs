using My_Orm_Data_Access.Driver;
using Npgsql;
using Testcontainers.PostgreSql;

namespace My_Orm_postgres.Tests;

// Um Postgres descartável (Docker) para todos os testes: nunca toca no banco de desenvolvimento
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString => container.GetConnectionString();

    // Uma sessão nova do ORM, como a API cria a cada requisição
    public Postgres NewSession() => new(ConnectionString);

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Schema real da API (Database/*.sql) + uma tabela só para testar tipos de dado
        var scripts = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Database"), "*.sql")
            .Order()
            .Select(File.ReadAllText)
            .Append(TipoTesteSql);

        foreach (string sql in scripts)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    private const string TipoTesteSql = """
        CREATE TABLE tipo_teste (
            id          BIGSERIAL     PRIMARY KEY,
            valor       NUMERIC(12,2) NOT NULL,
            quando      TIMESTAMPTZ   NOT NULL,
            dia         DATE          NOT NULL,
            codigo      UUID          NOT NULL,
            status      INT           NOT NULL,
            dados       BYTEA,
            obs         TEXT,
            inserted_at BIGINT        NOT NULL,
            updated_at  BIGINT        NOT NULL
        );
        """;
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

// E-mail único por teste: os testes dividem o banco e não dependem de ordem nem de limpeza
public static class Unico
{
    public static string Email() => $"{Guid.NewGuid():N}@teste.com";
}
