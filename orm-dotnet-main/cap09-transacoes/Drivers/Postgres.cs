using System.Data;
using Npgsql;
using MeuOrm.Core;

namespace MeuOrm.Drivers;

public class Postgres : IOrm
{
    private readonly string connectionString;
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;

    // O "painel" da última operação
    private string dbError = "";
    private string lastSql = "";
    private long rowsAffected;
    private long generatedId;
    private DataTable dataTable = new();

    public Postgres()
    {
        connectionString =
            $"Host={DotEnv.Get("DB_HOST")};" +
            $"Port={DotEnv.Get("DB_PORT")};" +
            $"Username={DotEnv.Get("DB_USER")};" +
            $"Password={DotEnv.Get("DB_PASSWORD")};" +
            $"Database={DotEnv.Get("DB_NAME")};" +
            "Timezone=UTC";
    }

    private void Connect()
    {
        connection = new NpgsqlConnection(connectionString);
        connection.Open();
    }

    private void Disconnect()
    {
        connection?.Dispose();
        connection = null;
    }

    public void Exec(string sql, string action, Dictionary<string, object?>? args = null)
    {
        // 1. Zera o painel: cada execução começa limpa
        lastSql = sql;
        dbError = "";
        rowsAffected = 0;
        generatedId = 0;
        dataTable = new DataTable();

        // 2. Sem transação manual aberta, este Exec abre e fecha sua própria conexão
        bool ownConnection = transaction == null;

        try
        {
            if (ownConnection)
                Connect();

            // 3. Monta o comando e adiciona os parâmetros
            using var cmd = new NpgsqlCommand(sql, connection, transaction);

            if (args != null)
                foreach (var (name, value) in args)
                    cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

            // 4. Escolhe o jeito certo de executar
            switch (action)
            {
                case "query":
                    using (var reader = cmd.ExecuteReader())
                        dataTable.Load(reader);
                    rowsAffected = dataTable.Rows.Count;
                    break;

                case "insert":
                    using (var reader = cmd.ExecuteReader())
                        dataTable.Load(reader);
                    rowsAffected = dataTable.Rows.Count;
                    if (rowsAffected > 0 && dataTable.Columns.Contains("id"))
                        generatedId = Convert.ToInt64(dataTable.Rows[0]["id"]);
                    break;

                case "update":
                case "delete":
                case "exec":
                    rowsAffected = cmd.ExecuteNonQuery();
                    break;

                default:
                    throw new ArgumentException($"Ação inválida: '{action}'. " +
                        "Use query, insert, update, delete ou exec.");
            }
        }
        catch (NpgsqlException ex)
        {
            dbError = ex.Message;
            throw new OrmException(OrmErrorKind.Other, ex.Message, inner: ex);
        }
        finally
        {
            // 5. Fecha a conexão que ele mesmo abriu, dando certo ou errado
            if (ownConnection)
                Disconnect();
        }
    }

    public string GetStatus() => dbError == "" ? "success" : "fail";
    public string GetDbError() => dbError;
    public long GetRowsAffected() => rowsAffected;
    public long GetGeneratedId() => generatedId;
    public DataTable GetDataTable() => dataTable;
    public string GetLastSqlSentence() => lastSql;

    // ===== Capítulo 8: CRUD =====
    public DataTable Select(string table, string fields = "*", string where = "",
                            Dictionary<string, object?>? args = null, string order = "")
    {
        string sql = $"SELECT {fields} FROM {table}";

        if (where != "")
            sql += $" WHERE {where}";

        if (order != "")
            sql += $" ORDER BY {order}";

        Exec(sql, "query", args);
        return dataTable;
    }

    public void Insert(string table, Dictionary<string, object?> data)
    {
        // data = { ["nome"] = "Maria", ["email"] = "maria@email.com" }
        string fields = string.Join(", ", data.Keys);                   // nome, email
        string values = string.Join(", ", data.Keys.Select(k => "@" + k)); // @nome, @email

        Exec($"INSERT INTO {table} ({fields}) VALUES ({values}) RETURNING *", "insert", data);
    }

    public void Update(string table, Dictionary<string, object?> data, string where,
                       Dictionary<string, object?>? args = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException(
                "Update sem WHERE bloqueado: isso alteraria a tabela inteira.");

        // Junta os parâmetros do WHERE com os valores novos
        var parameters = new Dictionary<string, object?>(args ?? new Dictionary<string, object?>());
        var sets = new List<string>();

        foreach (var (column, value) in data)
        {
            sets.Add($"{column} = @v_{column}");   // nome = @v_nome
            parameters["v_" + column] = value;
        }

        Exec($"UPDATE {table} SET {string.Join(", ", sets)} WHERE {where}", "update", parameters);
    }

    public void Delete(string table, string where, Dictionary<string, object?>? args = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException(
                "Delete sem WHERE bloqueado: isso apagaria a tabela inteira.");

        Exec($"DELETE FROM {table} WHERE {where}", "delete", args);
    }

    // ===== Capítulo 9: transações =====
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
            return; // nada a desfazer

        transaction.Rollback();
        EndTransaction();
    }

    private void EndTransaction()
    {
        transaction?.Dispose();
        transaction = null;
        Disconnect();
    }

    public void Dispose()
    {
        // Esqueceram o Commit? Desfaz tudo. Nunca grave pela metade.
        try { Rollback(); }
        catch { /* a conexão pode já ter caído; o banco desfaz sozinho */ }

        Disconnect();
    }
}
