using My_Orm_Data_Access.Core;
using System.Data.Common;
using Npgsql;
namespace My_Orm_Data_Access.Driver;

public class Postgres : DriverBase
{ 
    private readonly string connectionString;

    public Postgres(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override DbConnection CreateConnection()
     => new NpgsqlConnection(connectionString);

    protected override string InsertSql(string table, string fields, string values)
     => $"INSERT INTO {table} ({fields}) VALUES ({values}) RETURNING *";

    protected override void ReadInsertResult(DbCommand cmd)
    {
        using (var reader = cmd.ExecuteReader())
            dataTable.Load(reader);

        rowsAffected = dataTable.Rows.Count;

        if (rowsAffected > 0 && dataTable.Columns.Contains("id"))
            generatedId = Convert.ToInt64(dataTable.Rows[0]["id"]);
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
