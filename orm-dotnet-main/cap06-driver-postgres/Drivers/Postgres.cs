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

    // Preenchidos nos capítulos 8 e 9
    public DataTable Select(string table, string fields = "*", string where = "",
        Dictionary<string, object?>? args = null, string order = "") => throw new NotImplementedException();
    public void Insert(string table, Dictionary<string, object?> data) => throw new NotImplementedException();
    public void Update(string table, Dictionary<string, object?> data, string where,
        Dictionary<string, object?>? args = null) => throw new NotImplementedException();
    public void Delete(string table, string where,
        Dictionary<string, object?>? args = null) => throw new NotImplementedException();
    public void BeginTransaction() => throw new NotImplementedException();
    public void Commit() => throw new NotImplementedException();
    public void Rollback() => throw new NotImplementedException();
    public void Dispose() => Disconnect();
}
