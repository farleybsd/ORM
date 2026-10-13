using System.Data;
using System.Data.Common;
using MeuOrm.Core;

namespace MeuOrm.Drivers;

public abstract class DriverBase : IOrm
{
    protected DbConnection? connection;
    protected DbTransaction? transaction;

    protected string dbError = "";
    protected string lastSql = "";
    protected long rowsAffected;
    protected long generatedId;
    protected DataTable dataTable = new();

    // ===== Os buracos que cada banco preenche =====
    protected abstract DbConnection CreateConnection();
    protected abstract string InsertSql(string table, string fields, string values);
    protected abstract void ReadInsertResult(DbCommand cmd);
    protected abstract OrmException TranslateError(DbException ex);

    // ===== O roteiro comum =====
    public void Exec(string sql, string action, Dictionary<string, object?>? args = null)
    {
        lastSql = sql;
        dbError = "";
        rowsAffected = 0;
        generatedId = 0;
        dataTable = new DataTable();

        bool ownConnection = transaction == null;

        try
        {
            if (ownConnection)
                Connect();

            using DbCommand cmd = connection!.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction;

            if (args != null)
            {
                foreach (var (name, value) in args)
                {
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = "@" + name;
                    p.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
            }

            switch (action)
            {
                case "query":
                    using (var reader = cmd.ExecuteReader())
                        dataTable.Load(reader);
                    rowsAffected = dataTable.Rows.Count;
                    break;

                case "insert":
                    ReadInsertResult(cmd);          // cada banco pega o id do seu jeito
                    break;

                case "update":
                case "delete":
                case "exec":
                    rowsAffected = cmd.ExecuteNonQuery();
                    break;

                default:
                    throw new ArgumentException($"Ação inválida: '{action}'.");
            }
        }
        catch (DbException ex)
        {
            OrmException error = TranslateError(ex);  // cada banco traduz seus códigos
            dbError = error.Message;
            throw error;
        }
        finally
        {
            if (ownConnection)
                Disconnect();
        }
    }

    public DataTable Select(string table, string fields = "*", string where = "",
                            Dictionary<string, object?>? args = null, string order = "")
    {
        string sql = $"SELECT {fields} FROM {table}";
        if (where != "") sql += $" WHERE {where}";
        if (order != "") sql += $" ORDER BY {order}";

        Exec(sql, "query", args);
        return dataTable;
    }

    public void Insert(string table, Dictionary<string, object?> data)
    {
        string fields = string.Join(", ", data.Keys);
        string values = string.Join(", ", data.Keys.Select(k => "@" + k));
        Exec(InsertSql(table, fields, values), "insert", data);
    }

    public void Update(string table, Dictionary<string, object?> data, string where,
                       Dictionary<string, object?>? args = null)
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

        Exec($"UPDATE {table} SET {string.Join(", ", sets)} WHERE {where}", "update", parameters);
    }

    public void Delete(string table, string where, Dictionary<string, object?>? args = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("Delete sem WHERE bloqueado.");

        Exec($"DELETE FROM {table} WHERE {where}", "delete", args);
    }

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

    private void EndTransaction()
    {
        transaction?.Dispose();
        transaction = null;
        Disconnect();
    }

    private void Connect()
    {
        connection = CreateConnection();
        connection.Open();
    }

    private void Disconnect()
    {
        connection?.Dispose();
        connection = null;
    }

    public string GetStatus() => dbError == "" ? "success" : "fail";
    public string GetDbError() => dbError;
    public long GetRowsAffected() => rowsAffected;
    public long GetGeneratedId() => generatedId;
    public DataTable GetDataTable() => dataTable;
    public string GetLastSqlSentence() => lastSql;

    public void Dispose()
    {
        try { Rollback(); }
        catch { /* conexão já caída: o banco desfaz sozinho */ }

        Disconnect();
    }
}
