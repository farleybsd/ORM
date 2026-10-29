using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Mapping;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace My_Orm_Data_Access.Driver;

public abstract class DriverBase : IOrm
{
    protected DbConnection? connection;
    protected DbTransaction? transaction;

    private readonly ILogger logger;
    private readonly TimeSpan slowQueryThreshold;

    protected DriverBase(ILogger? logger, TimeSpan? slowQueryThreshold = null)
    {
        this.logger = logger ?? NullLogger.Instance;
        this.slowQueryThreshold = slowQueryThreshold ?? OrmOptions.DefaultSlowQueryThreshold;
    }

    // ===== Os buracos que cada banco preenche =====
    protected abstract DbConnection CreateConnection();
    protected abstract string InsertSql(string table, string fields, string values);
    protected abstract long ExecuteInsert(DbCommand cmd);   // executa o INSERT e devolve o id gerado
    protected abstract Task<long> ExecuteInsertAsync(DbCommand cmd, CancellationToken ct);
    protected abstract OrmException TranslateError(DbException ex);
    protected abstract string QuoteIdentifier(string name); // cada banco usa suas aspas: "x", [x], `x`

    // ===== Transação =====
    public void BeginTransaction()
    {
        EnsureNoTransaction();
        Connect();
        transaction = connection!.BeginTransaction();
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        EnsureNoTransaction();
        await ConnectAsync(ct);
        transaction = await connection!.BeginTransactionAsync(ct);
    }

    public void Commit()
    {
        EnsureTransaction().Commit();
        EndTransaction();
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await EnsureTransaction().CommitAsync(ct);
        await EndTransactionAsync();
    }

    public void Rollback()
    {
        if (transaction == null)
            return;

        transaction.Rollback();
        EndTransaction();
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (transaction == null)
            return;

        await transaction.RollbackAsync(ct);
        await EndTransactionAsync();
    }

    // ===== SQL livre =====
    public DataTable Query(string sql, Dictionary<string, object?>? args = null)
    {
        return Run(sql, args, cmd =>
        {
            using var reader = cmd.ExecuteReader();
            var table = CreateTable(reader);
            while (reader.Read())
                AddRow(table, reader);
            return table;
        });
    }

    public Task<DataTable> QueryAsync(string sql, Dictionary<string, object?>? args = null,
                                      CancellationToken ct = default)
    {
        return RunAsync(sql, args, async (cmd, c) =>
        {
            await using var reader = await cmd.ExecuteReaderAsync(c);
            var table = CreateTable(reader);
            while (await reader.ReadAsync(c))
                AddRow(table, reader);
            return table;
        }, ct);
    }

    public List<T> Query<T>(string sql, Dictionary<string, object?>? args = null)
    {
        return Run(sql, args, cmd =>
        {
            using var reader = cmd.ExecuteReader();
            var map = RowMapper.Create<T>(reader);
            var list = new List<T>();
            while (reader.Read())
                list.Add(map(reader));
            return list;
        });
    }

    public Task<List<T>> QueryAsync<T>(string sql, Dictionary<string, object?>? args = null,
                                       CancellationToken ct = default)
    {
        return RunAsync(sql, args, async (cmd, c) =>
        {
            await using var reader = await cmd.ExecuteReaderAsync(c);
            var map = RowMapper.Create<T>(reader);
            var list = new List<T>();
            while (await reader.ReadAsync(c))
                list.Add(map(reader));
            return list;
        }, ct);
    }

    public T? QueryFirst<T>(string sql, Dictionary<string, object?>? args = null)
    {
        return Run(sql, args, cmd =>
        {
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? RowMapper.Create<T>(reader)(reader) : default;
        });
    }

    public Task<T?> QueryFirstAsync<T>(string sql, Dictionary<string, object?>? args = null,
                                       CancellationToken ct = default)
    {
        return RunAsync(sql, args, async (cmd, c) =>
        {
            await using var reader = await cmd.ExecuteReaderAsync(c);
            return await reader.ReadAsync(c) ? RowMapper.Create<T>(reader)(reader) : default;
        }, ct);
    }

    public long Execute(string sql, Dictionary<string, object?>? args = null)
        => Run(sql, args, cmd => (long)cmd.ExecuteNonQuery());

    public Task<long> ExecuteAsync(string sql, Dictionary<string, object?>? args = null,
                                   CancellationToken ct = default)
        => RunAsync(sql, args, async (cmd, c) => (long)await cmd.ExecuteNonQueryAsync(c), ct);

    // ===== CRUD =====
    public DataTable Select(string table, string fields = "*", string where = "", Dictionary<string, object?>? args = null, string order = "")
        => Query(SelectSql(table, fields, where, order), args);

    public Task<DataTable> SelectAsync(string table, string fields = "*", string where = "",
                                       Dictionary<string, object?>? args = null, string order = "",
                                       CancellationToken ct = default)
        => QueryAsync(SelectSql(table, fields, where, order), args, ct);

    public long Insert(string table, Dictionary<string, object?> data)
        => Run(InsertSql(table, data), data, ExecuteInsert);

    public Task<long> InsertAsync(string table, Dictionary<string, object?> data,
                                  CancellationToken ct = default)
        => RunAsync(InsertSql(table, data), data, ExecuteInsertAsync, ct);

    public long Update(string table, Dictionary<string, object?> data, string where, Dictionary<string, object?>? args = null)
    {
        var (sql, parameters) = UpdateSql(table, data, where, args);
        return Execute(sql, parameters);
    }

    public Task<long> UpdateAsync(string table, Dictionary<string, object?> data, string where,
                                  Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        var (sql, parameters) = UpdateSql(table, data, where, args);
        return ExecuteAsync(sql, parameters, ct);
    }

    public long Delete(string table, string where, Dictionary<string, object?>? args = null)
        => Execute(DeleteSql(table, where), args);

    public Task<long> DeleteAsync(string table, string where, Dictionary<string, object?>? args = null,
                                  CancellationToken ct = default)
        => ExecuteAsync(DeleteSql(table, where), args, ct);

    // ===== Fim da sessão: o que não teve Commit é desfeito =====
    public void Dispose()
    {
        try { Rollback(); }
        catch { /* conexão já caída: o banco desfaz sozinho */ }

        Disconnect();
    }

    public async ValueTask DisposeAsync()
    {
        try { await RollbackAsync(); }
        catch { /* conexão já caída: o banco desfaz sozinho */ }

        await DisconnectAsync();
    }

    // ===== Montagem do SQL (igual para as versões síncrona e Async) =====
    // Nomes de tabela e coluna sempre passam por Table()/Column(): validados e entre aspas.
    // fields, where e order continuam SQL livre: valores ali devem ir sempre por parâmetro.
    private string SelectSql(string table, string fields, string where, string order)
    {
        string sql = $"SELECT {fields} FROM {Table(table)}";
        if (where != "") sql += $" WHERE {where}";
        if (order != "") sql += $" ORDER BY {order}";
        return sql;
    }

    private string InsertSql(string table, Dictionary<string, object?> data)
    {
        string fields = string.Join(", ", data.Keys.Select(Column));
        string values = string.Join(", ", data.Keys.Select(k => "@" + k));
        return InsertSql(Table(table), fields, values);
    }

    private (string sql, Dictionary<string, object?> parameters) UpdateSql(
        string table, Dictionary<string, object?> data, string where, Dictionary<string, object?>? args)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("Update sem WHERE bloqueado.");

        var parameters = new Dictionary<string, object?>(args ?? new Dictionary<string, object?>());
        var sets = new List<string>();

        foreach (var (column, value) in data)
        {
            sets.Add($"{Column(column)} = @v_{column}");
            parameters["v_" + column] = value;
        }

        return ($"UPDATE {Table(table)} SET {string.Join(", ", sets)} WHERE {where}", parameters);
    }

    private string DeleteSql(string table, string where)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("Delete sem WHERE bloqueado.");

        return $"DELETE FROM {Table(table)} WHERE {where}";
    }

    // ===== Quoting de identificadores =====

    // Aceita "cliente" ou "schema.cliente": cada parte é validada e posta entre aspas
    private string Table(string name)
        => string.Join(".", name.Split('.').Select(part => QuoteIdentifier(ValidName(part, "tabela"))));

    // A coluna também vira nome de parâmetro (@coluna), por isso a validação é a mesma
    private string Column(string name)
        => QuoteIdentifier(ValidName(name, "coluna"));

    // Só letras, números e "_", sem começar com número: barra ";", ")", "--", espaço etc.
    private static string ValidName(string name, string kind)
    {
        bool valid = name.Length > 0
            && !char.IsDigit(name[0])
            && name.All(c => char.IsLetterOrDigit(c) || c == '_');

        if (!valid)
            throw new ArgumentException(
                $"Nome de {kind} inválido: '{name}'. Use só letras, números e '_' (sem começar com número).");

        return name;
    }

    // ===== A porta única: todo SQL passa por aqui (conexão, parâmetros, log, tempo e tradução de erro) =====
    // O tempo conta desde pegar a conexão do pool até ler a última linha
    private T Run<T>(string sql, Dictionary<string, object?>? args, Func<DbCommand, T> action)
    {
        bool ownConnection = transaction == null;
        var watch = Stopwatch.StartNew();

        try
        {
            if (ownConnection) // Conexão própria
                Connect();

            using DbCommand cmd = CreateCommand(sql, args);
            T result = action(cmd);
            LogSql(sql, watch.Elapsed);
            return result;
        }
        catch (DbException ex)
        {
            throw Fail(ex, sql, watch.Elapsed);
        }
        finally
        {
            if (ownConnection)
                Disconnect();
        }
    }

    private async Task<T> RunAsync<T>(string sql, Dictionary<string, object?>? args,
                                      Func<DbCommand, CancellationToken, Task<T>> action,
                                      CancellationToken ct)
    {
        bool ownConnection = transaction == null;
        var watch = Stopwatch.StartNew();

        try
        {
            if (ownConnection) // Conexão própria
                await ConnectAsync(ct);

            await using DbCommand cmd = CreateCommand(sql, args);
            T result = await action(cmd, ct);
            LogSql(sql, watch.Elapsed);
            return result;
        }
        catch (DbException ex)
        {
            throw Fail(ex, sql, watch.Elapsed);
        }
        finally
        {
            if (ownConnection)
                await DisconnectAsync();
        }
    }

    private DbCommand CreateCommand(string sql, Dictionary<string, object?>? args)
    {
        DbCommand cmd = connection!.CreateCommand();
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

        return cmd;
    }

    // Só o SQL vai para o log, nunca os valores dos parâmetros (podem ter dados pessoais)
    private void LogSql(string sql, TimeSpan elapsed)
    {
        long ms = (long)elapsed.TotalMilliseconds;

        // Lento sai como Warning: aparece em produção mesmo com o Debug desligado
        if (elapsed >= slowQueryThreshold)
            logger.LogWarning("SQL lento ({ElapsedMs} ms, limite {LimitMs} ms): {Sql}",
                ms, (long)slowQueryThreshold.TotalMilliseconds, sql);
        else
            logger.LogDebug("SQL ({ElapsedMs} ms): {Sql}", ms, sql);
    }

    private OrmException Fail(DbException ex, string sql, TimeSpan elapsed)
    {
        OrmException error = TranslateError(ex);  // cada banco traduz seus códigos
        logger.LogWarning(ex, "Falha no SQL ({Kind}, {ElapsedMs} ms): {Sql}",
            error.Kind, (long)elapsed.TotalMilliseconds, sql);
        return error;
    }

    // ===== Leitura das linhas (o DataTable.Load só existe na versão síncrona) =====
    private static DataTable CreateTable(DbDataReader reader)
    {
        var table = new DataTable();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            // Em JOINs a mesma coluna pode vir duas vezes (ex.: "id"): a segunda vira "id1"
            string name = reader.GetName(i);
            string unique = name;
            for (int n = 1; table.Columns.Contains(unique); n++)
                unique = $"{name}{n}";

            table.Columns.Add(unique, reader.GetFieldType(i));
        }

        return table;
    }

    private static void AddRow(DataTable table, DbDataReader reader)
    {
        var values = new object[reader.FieldCount];
        reader.GetValues(values);
        table.Rows.Add(values);
    }

    // ===== Conexão e transação =====
    private void EnsureNoTransaction()
    {
        if (transaction != null)
            throw new InvalidOperationException("Já existe uma transação aberta.");
    }

    private DbTransaction EnsureTransaction()
    {
        return transaction
            ?? throw new InvalidOperationException("Nenhuma transação aberta para confirmar.");
    }

    private void Connect()
    {
        connection = CreateConnection();
        connection.Open();
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        connection = CreateConnection();
        await connection.OpenAsync(ct);
    }

    private void EndTransaction()
    {
        transaction?.Dispose();
        transaction = null;
        Disconnect();
    }

    private async Task EndTransactionAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
        transaction = null;
        await DisconnectAsync();
    }

    private void Disconnect()
    {
        connection?.Dispose();
        connection = null;
    }

    private async Task DisconnectAsync()
    {
        if (connection != null)
            await connection.DisposeAsync();
        connection = null;
    }
}
