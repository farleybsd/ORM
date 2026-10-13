using System.Data.Common;
using MySqlConnector;
using MeuOrm.Core;

namespace MeuOrm.Drivers;

public class MySql : DriverBase
{
    private readonly string connectionString;

    public MySql()
    {
        connectionString =
            $"Server={DotEnv.Get("DB_HOST")};Port={DotEnv.Get("DB_PORT")};" +
            $"User ID={DotEnv.Get("DB_USER")};Password={DotEnv.Get("DB_PASSWORD")};" +
            $"Database={DotEnv.Get("DB_NAME")}";
    }

    protected override DbConnection CreateConnection()
        => new MySqlConnection(connectionString);

    // MySQL não tem RETURNING
    protected override string InsertSql(string table, string fields, string values)
        => $"INSERT INTO {table} ({fields}) VALUES ({values})";

    // ...então o id vem de uma propriedade do próprio comando
    protected override void ReadInsertResult(DbCommand cmd)
    {
        rowsAffected = cmd.ExecuteNonQuery();
        generatedId = ((MySqlCommand)cmd).LastInsertedId;
    }

    protected override OrmException TranslateError(DbException ex)
    {
        if (ex is not MySqlException my)
            return new OrmException(OrmErrorKind.Connection, ex.Message, inner: ex);

        return my.Number switch
        {
            1062         => new OrmException(OrmErrorKind.Duplicate, "Registro duplicado", inner: my),
            1451 or 1452 => new OrmException(OrmErrorKind.ForeignKey, "Referência inválida", inner: my),
            1048         => new OrmException(OrmErrorKind.NotNull, my.Message, inner: my),
            1146         => new OrmException(OrmErrorKind.TableNotFound, my.Message, inner: my),
            1054         => new OrmException(OrmErrorKind.ColumnNotFound, my.Message, inner: my),
            1213         => new OrmException(OrmErrorKind.Deadlock, "Deadlock detectado", inner: my),
            1042 or 1045 => new OrmException(OrmErrorKind.Connection, my.Message, inner: my),
            _            => new OrmException(OrmErrorKind.Other, my.Message, inner: my)
        };
    }
}
