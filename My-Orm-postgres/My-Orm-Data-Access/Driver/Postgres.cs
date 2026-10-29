using Microsoft.Extensions.Logging;
using My_Orm_Data_Access.Core;
using System.Data.Common;
using Npgsql;
namespace My_Orm_Data_Access.Driver;

public class Postgres : DriverBase
{ 
    private readonly string connectionString;

    public Postgres(string connectionString, ILogger<Postgres>? logger = null)
        : base(logger)
    {
        this.connectionString = connectionString;
    }

    protected override DbConnection CreateConnection()
     => new NpgsqlConnection(connectionString);

    protected override string InsertSql(string table, string fields, string values)
     => $"INSERT INTO {table} ({fields}) VALUES ({values}) RETURNING *";

    // Postgres usa aspas duplas; uma aspa dentro do nome vira duas para não "fechar" o nome
    protected override string QuoteIdentifier(string name)
     => "\"" + name.Replace("\"", "\"\"") + "\"";

    protected override long ExecuteInsert(DbCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadId(reader) : 0;
    }

    protected override async Task<long> ExecuteInsertAsync(DbCommand cmd, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadId(reader) : 0;
    }

    // RETURNING * devolve a linha inteira: procura a coluna "id", se existir
    private static long ReadId(DbDataReader reader)
    {
        for (int i = 0; i < reader.FieldCount; i++)
            if (reader.GetName(i) == "id" && !reader.IsDBNull(i))
                return Convert.ToInt64(reader.GetValue(i));

        return 0;
    }

    protected override OrmException TranslateError(DbException ex)
    {
        if (ex is not PostgresException pg)
            return new OrmException(OrmErrorKind.Connection,
                $"Falha de conexão com o banco: {ex.Message}", inner: ex);

        return pg.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new OrmException(OrmErrorKind.Duplicate,
                $"Registro duplicado ({pg.ConstraintName})", pg.ConstraintName ?? "", pg),
            PostgresErrorCodes.ForeignKeyViolation => new OrmException(OrmErrorKind.ForeignKey,
                $"Referência inválida ({pg.ConstraintName})", pg.ConstraintName ?? "", pg),
            PostgresErrorCodes.NotNullViolation => new OrmException(OrmErrorKind.NotNull,
                $"Campo obrigatório não informado: {pg.ColumnName}", pg.ColumnName ?? "", pg),
            PostgresErrorCodes.UndefinedTable => new OrmException(OrmErrorKind.TableNotFound,
                pg.MessageText, inner: pg),
            PostgresErrorCodes.UndefinedColumn => new OrmException(OrmErrorKind.ColumnNotFound,
                pg.MessageText, inner: pg),
            PostgresErrorCodes.DeadlockDetected => new OrmException(OrmErrorKind.Deadlock,
                "Deadlock detectado. Tente a operação novamente.", inner: pg),
            _ => new OrmException(OrmErrorKind.Other, pg.MessageText, inner: pg)
        };
    }
}
