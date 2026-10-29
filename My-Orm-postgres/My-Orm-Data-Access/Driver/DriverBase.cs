using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using My_Orm_Data_Access.Core;
using System.Data;
using System.Data.Common;

namespace My_Orm_Data_Access.Driver;

public abstract class DriverBase : IOrm
{
    protected DbConnection? connection;
    protected DbTransaction? transaction;

    private readonly ILogger logger;

    protected DriverBase(ILogger? logger)
    {
        this.logger = logger ?? NullLogger.Instance;
    }

    // ===== Os buracos que cada banco preenche =====
    protected abstract DbConnection CreateConnection();
    protected abstract string InsertSql(string table, string fields, string values);
    protected abstract long ExecuteInsert(DbCommand cmd);   // executa o INSERT e devolve o id gerado
    protected abstract OrmException TranslateError(DbException ex);

    public void BeginTransaction()
    {
        if (transaction != null)
            throw new InvalidOperationException("Já existe uma transação aberta.");

        Connect();
        transaction = connection!.BeginTransaction();
    }

    public void Commit()
    {
        if (transaction == null)
            throw new InvalidOperationException("Nenhuma transação aberta para confirmar.");
        transaction.Commit();
        EndTransaction();
    }

    public void Rollback()
    {
        if (transaction == null)
            return;

        transaction.Rollback();
        EndTransaction();
    }

    public DataTable Query(string sql, Dictionary<string, object?>? args = null)
    {
        return Run(sql, args, cmd =>
        {
            var table = new DataTable();
            using (var reader = cmd.ExecuteReader())
                table.Load(reader);
            return table;
        });
    }

    public long Execute(string sql, Dictionary<string, object?>? args = null)
        => Run(sql, args, cmd => (long)cmd.ExecuteNonQuery());

    public DataTable Select(string table, string fields = "*", string where = "", Dictionary<string, object?>? args = null, string order = "")
    {
        string sql = $"SELECT {fields} FROM {table}";
        if (where != "") sql += $" WHERE {where}";
        if (order != "") sql += $" ORDER BY {order}";

        return Query(sql, args);
    }

    public long Insert(string table, Dictionary<string, object?> data)
    {
        string fields = string.Join(", ", data.Keys);
        string values = string.Join(", ", data.Keys.Select(k => "@" + k));
        return Run(InsertSql(table, fields, values), data, ExecuteInsert);
    }

    public long Update(string table, Dictionary<string, object?> data, string where, Dictionary<string, object?>? args = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("Update sem WHERE bloqueado.");

        var parameters = new Dictionary<string, object?>(args ?? new Dictionary<string, object?>());
        var sets = new List<string>();

        foreach (var (column, value) in data)
        {
            sets.Add($"{column} = @v_{column}");
            parameters["v_" + column] = value;
        }

        return Execute($"UPDATE {table} SET {string.Join(", ", sets)} WHERE {where}", parameters);
    }

    public long Delete(string table, string where, Dictionary<string, object?>? args = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("Delete sem WHERE bloqueado.");

        return Execute($"DELETE FROM {table} WHERE {where}", args);
    }

    public void Dispose()
    {
        try { Rollback(); }
        catch { /* conexão já caída: o banco desfaz sozinho */ }

        Disconnect();
    }

    // A porta única: todo SQL passa por aqui (conexão, parâmetros, log e tradução de erro)
    private T Run<T>(string sql, Dictionary<string, object?>? args, Func<DbCommand, T> action)
    {
        logger.LogDebug("SQL: {Sql}", sql);

        bool ownConnection = transaction == null;

        try
        {
            if (ownConnection) // Conexão própria
                Connect();

            using DbCommand cmd = connection!.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction;

            if (args != null) // Verifca se ha parametros para adicionar
            {
                foreach (var (name, value) in args)
                {
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = "@" + name;
                    p.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
            }

            return action(cmd);
        }
        catch (DbException ex)
        {
            OrmException error = TranslateError(ex);  // cada banco traduz seus códigos
            logger.LogWarning(ex, "Falha no SQL ({Kind}): {Sql}", error.Kind, sql);
            throw error;
        }
        finally
        {
            if (ownConnection)
                Disconnect();
        }
    }

    private void Connect()
    {
        connection = CreateConnection();
        connection.Open();
    }

    private void EndTransaction()
    {
        transaction?.Dispose();
        transaction = null;
        Disconnect();
    }

    private void Disconnect()
    {
        connection?.Dispose();
        connection = null;
    }
}
