using System.Data.Common;
using Npgsql;

namespace DDDWithHttp.Orm.Dialects;

internal sealed class PostgresDialect : ISqlDialect
{
    public DatabaseProvider Provider => DatabaseProvider.Postgres;

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public void BindValue(DbParameter parameter, object? value)
    {
        parameter.Value = value switch
        {
            null => DBNull.Value,
            Enum e => e.ToString(),   // convenção: enum é gravado como texto (legível e estável)
            // timestamptz só aceita UTC; Unspecified é tratado como UTC (convenção do ORM)
            DateTime dt when dt.Kind == DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTime dt => dt.ToUniversalTime(),
            _ => value                // Guid, DateOnly, decimal...: o Npgsql mapeia nativamente
        };
    }

    public OrmException Translate(DbException exception)
    {
        if (exception is not PostgresException pg)
            return exception is NpgsqlException { InnerException: TimeoutException }
                ? new OrmException(OrmErrorKind.Timeout, "O comando excedeu o tempo limite.", inner: exception)
                : new OrmException(OrmErrorKind.Connection, $"Falha de conexão com o Postgres: {exception.Message}", inner: exception);

        return pg.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new(OrmErrorKind.Duplicate,
                $"Registro duplicado ({pg.ConstraintName}).", pg.ConstraintName, pg),
            PostgresErrorCodes.ForeignKeyViolation => new(OrmErrorKind.ForeignKey,
                $"Referência inválida ({pg.ConstraintName}).", pg.ConstraintName, pg),
            PostgresErrorCodes.NotNullViolation => new(OrmErrorKind.NotNull,
                $"Campo obrigatório não informado: {pg.ColumnName}.", pg.ColumnName, pg),
            PostgresErrorCodes.CheckViolation => new(OrmErrorKind.CheckViolation,
                $"Regra do banco violada ({pg.ConstraintName}).", pg.ConstraintName, pg),
            PostgresErrorCodes.UndefinedTable => new(OrmErrorKind.TableNotFound, pg.MessageText, inner: pg),
            PostgresErrorCodes.UndefinedColumn => new(OrmErrorKind.ColumnNotFound, pg.MessageText, inner: pg),
            PostgresErrorCodes.DeadlockDetected => new(OrmErrorKind.Deadlock,
                "Deadlock detectado. A transação foi desfeita.", inner: pg),
            PostgresErrorCodes.SerializationFailure => new(OrmErrorKind.SerializationFailure,
                "Conflito de serialização. A transação foi desfeita.", inner: pg),
            PostgresErrorCodes.QueryCanceled => new(OrmErrorKind.Timeout, "O comando excedeu o tempo limite.", inner: pg),
            _ => new(OrmErrorKind.Other, pg.MessageText, inner: pg)
        };
    }
}
