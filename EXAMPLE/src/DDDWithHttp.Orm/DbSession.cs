using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DDDWithHttp.Orm.Configuration;
using DDDWithHttp.Orm.Dialects;
using Microsoft.Extensions.Logging;

namespace DDDWithHttp.Orm;

internal sealed partial class DbSession : IDbSession
{
    private const string ParameterPrefix = "@";
    private const string SetParameterPrefix = "v_";   // separa os valores do SET dos argumentos do WHERE
    private const string KeyParameter = "k_key";
    private const string VersionParameter = "k_version";

    private readonly ISqlDialect _dialect;
    private readonly OrmConnectionOptions _options;
    private readonly int _maxTransactionAttempts;
    private readonly ILogger _logger;

    private DbConnection? _connection;      // só existe enquanto há transação aberta
    private DbTransaction? _transaction;
    private int _operationInProgress;       // detecta uso concorrente da mesma sessão
    private bool _disposed;

    public DbSession(ISqlDialect dialect, OrmConnectionOptions options, int maxTransactionAttempts, ILogger logger)
    {
        _dialect = dialect;
        _options = options;
        _maxTransactionAttempts = Math.Max(1, maxTransactionAttempts);
        _logger = logger;
    }

    public DatabaseProvider Provider => _dialect.Provider;

    public bool HasActiveTransaction => _transaction is not null;

    // =====================================================================
    // SQL livre
    // =====================================================================

    public Task<IReadOnlyList<DbRow>> QueryAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default)
        => RunAsync<IReadOnlyList<DbRow>>(sql, args, async (cmd, token) =>
        {
            var rows = new List<DbRow>();
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
                rows.Add(DbRow.From(reader));

            return rows;
        }, ct);

    public async Task<DbRow?> QuerySingleOrDefaultAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        IReadOnlyList<DbRow> rows = await QueryAsync(sql, args, ct);

        return rows.Count switch
        {
            0 => null,
            1 => rows[0],
            _ => throw new InvalidOperationException($"A consulta deveria devolver no máximo 1 linha, mas devolveu {rows.Count}.")
        };
    }

    public Task<int> ExecuteAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default)
        => RunAsync(sql, args, (cmd, token) => cmd.ExecuteNonQueryAsync(token), ct);

    public Task<T?> ExecuteScalarAsync<T>(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default)
        => RunAsync(sql, args, async (cmd, token) =>
        {
            object? value = await cmd.ExecuteScalarAsync(token);
            return value is null or DBNull
                ? default
                : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }, ct);

    // =====================================================================
    // Atalhos de CRUD
    // =====================================================================

    public Task<int> InsertAsync(string table, Dictionary<string, object?> data, CancellationToken ct = default)
    {
        EnsureIdentifier(table);
        EnsureNotEmpty(data);
        foreach (string column in data.Keys) EnsureIdentifier(column);

        string columns = string.Join(", ", data.Keys);
        string values = string.Join(", ", data.Keys.Select(k => ParameterPrefix + k));

        return ExecuteAsync($"INSERT INTO {table} ({columns}) VALUES ({values})", data, ct);
    }

    public Task<int> UpdateAsync(string table, Dictionary<string, object?> data, string where,
                                 Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("UPDATE sem WHERE bloqueado.");

        EnsureIdentifier(table);
        EnsureNotEmpty(data);

        var parameters = new Dictionary<string, object?>(args ?? []);
        string sets = BuildSetClause(data, parameters);

        return ExecuteAsync($"UPDATE {table} SET {sets} WHERE {where}", parameters, ct);
    }

    public Task<int> DeleteAsync(string table, string where, Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new InvalidOperationException("DELETE sem WHERE bloqueado.");

        EnsureIdentifier(table);
        return ExecuteAsync($"DELETE FROM {table} WHERE {where}", args, ct);
    }

    public async Task<int> UpdateVersionedAsync(VersionedKey key, Dictionary<string, object?> data, CancellationToken ct = default)
    {
        EnsureIdentifier(key.Table);
        EnsureIdentifier(key.KeyColumn);
        EnsureIdentifier(key.VersionColumn);
        EnsureNotEmpty(data);

        var parameters = VersionedParameters(key);
        string sets = BuildSetClause(data, parameters);

        int rows = await ExecuteAsync(
            $"UPDATE {key.Table} SET {sets}, {key.VersionColumn} = {key.VersionColumn} + 1 " +
            $"WHERE {VersionedWhere(key)}", parameters, ct);

        EnsureVersionMatched(rows, key);
        return key.ExpectedVersion + 1;
    }

    public async Task DeleteVersionedAsync(VersionedKey key, CancellationToken ct = default)
    {
        EnsureIdentifier(key.Table);
        EnsureIdentifier(key.KeyColumn);
        EnsureIdentifier(key.VersionColumn);

        int rows = await ExecuteAsync($"DELETE FROM {key.Table} WHERE {VersionedWhere(key)}", VersionedParameters(key), ct);
        EnsureVersionMatched(rows, key);
    }

    private static string VersionedWhere(VersionedKey key)
        => $"{key.KeyColumn} = {ParameterPrefix}{KeyParameter} AND {key.VersionColumn} = {ParameterPrefix}{VersionParameter}";

    private static Dictionary<string, object?> VersionedParameters(VersionedKey key) => new()
    {
        [KeyParameter] = key.KeyValue,
        [VersionParameter] = key.ExpectedVersion
    };

    private static void EnsureVersionMatched(int rows, VersionedKey key)
    {
        if (rows == 0)
            throw new OrmException(OrmErrorKind.Concurrency,
                $"O registro {key.KeyValue} de '{key.Table}' foi alterado ou excluído por outro usuário " +
                $"(versão esperada: {key.ExpectedVersion}). Recarregue os dados e tente novamente.",
                key.Table);
    }

    private static string BuildSetClause(Dictionary<string, object?> data, Dictionary<string, object?> parameters)
    {
        var sets = new List<string>(data.Count);

        foreach (var (column, value) in data)
        {
            EnsureIdentifier(column);
            sets.Add($"{column} = {ParameterPrefix}{SetParameterPrefix}{column}");
            parameters[SetParameterPrefix + column] = value;
        }

        return string.Join(", ", sets);
    }

    // Nomes de tabela/coluna não podem ser parâmetros; por isso são validados contra injeção.
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$")]
    private static partial Regex IdentifierRegex();

    private static void EnsureIdentifier(string name)
    {
        if (!IdentifierRegex().IsMatch(name))
            throw new ArgumentException($"Identificador SQL inválido: '{name}'.", nameof(name));
    }

    private static void EnsureNotEmpty(Dictionary<string, object?> data)
    {
        if (data.Count == 0)
            throw new ArgumentException("Nenhuma coluna informada.", nameof(data));
    }

    // =====================================================================
    // Transação
    // =====================================================================

    public async Task<IOrmTransaction> BeginTransactionAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
            throw new InvalidOperationException(
                "Já existe uma transação aberta nesta sessão. Use ExecuteInTransactionAsync para participar dela.");

        DbConnection connection = await OpenConnectionAsync(ct);
        try
        {
            _transaction = await connection.BeginTransactionAsync(isolation, ct);
            _connection = connection;
        }
        catch (DbException ex)
        {
            await connection.DisposeAsync();
            throw _dialect.Translate(ex);
        }

        _logger.LogDebug("[{Provider}] BEGIN TRANSACTION ({Isolation})", Provider, isolation);
        return new OrmTransaction(this);
    }

    /// <summary>Chamado pelo <see cref="OrmTransaction"/>. Sempre libera a conexão, mesmo se o Commit falhar.</summary>
    internal async Task EndTransactionAsync(bool commit, CancellationToken ct)
    {
        if (_transaction is null)
            return;

        try
        {
            if (commit)
                await _transaction.CommitAsync(ct);
            else
                await _transaction.RollbackAsync(CancellationToken.None);  // rollback nunca é cancelado

            _logger.LogDebug("[{Provider}] {Outcome}", Provider, commit ? "COMMIT" : "ROLLBACK");
        }
        catch (DbException ex)
        {
            throw _dialect.Translate(ex);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;

            if (_connection is not null)
                await _connection.DisposeAsync();   // devolve ao pool
            _connection = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work,
                                                      IsolationLevel isolation = IsolationLevel.ReadCommitted,
                                                      CancellationToken ct = default)
    {
        // Transação já aberta (pelo caso de uso, por exemplo): participa dela.
        // Quem abriu é quem decide Commit/Rollback e quem faz retry.
        if (_transaction is not null)
            return await work(ct);

        for (int attempt = 1; ; attempt++)
        {
            await using (IOrmTransaction tx = await BeginTransactionAsync(isolation, ct))
            {
                try
                {
                    T result = await work(ct);
                    await tx.CommitAsync(ct);
                    return result;
                }
                catch (OrmException ex) when (ex.IsTransient && attempt < _maxTransactionAttempts)
                {
                    _logger.LogWarning(ex, "[{Provider}] {Kind} na tentativa {Attempt}/{Max}. Repetindo a transação.",
                        Provider, ex.Kind, attempt, _maxTransactionAttempts);
                }
                // qualquer outra exceção sai daqui e o "await using" faz Rollback
            }

            // O "await using" já fez Rollback e liberou a conexão. Espera um pouco antes de tentar de novo
            // (backoff com jitter para as transações em conflito não colidirem outra vez).
            await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt + Random.Shared.Next(0, 50)), ct);
        }
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work,
                                          IsolationLevel isolation = IsolationLevel.ReadCommitted,
                                          CancellationToken ct = default)
        => ExecuteInTransactionAsync<bool>(async token => { await work(token); return true; }, isolation, ct);

    // =====================================================================
    // Execução de comandos
    // =====================================================================

    private async Task<T> RunAsync<T>(string sql, Dictionary<string, object?>? args,
                                      Func<DbCommand, CancellationToken, Task<T>> run, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Interlocked.Exchange(ref _operationInProgress, 1) == 1)
            throw new InvalidOperationException(
                "Outra operação já está em andamento nesta sessão. Aguarde (await) cada chamada antes da próxima.");

        bool ownConnection = _transaction is null;
        DbConnection? connection = null;
        long start = Stopwatch.GetTimestamp();

        try
        {
            connection = ownConnection ? await OpenConnectionAsync(ct) : _connection!;

            await using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = _transaction;
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            if (args is not null)
            {
                foreach (var (name, value) in args)
                {
                    DbParameter parameter = cmd.CreateParameter();
                    parameter.ParameterName = ParameterPrefix + name;
                    _dialect.BindValue(parameter, value);
                    cmd.Parameters.Add(parameter);
                }
            }

            T result = await run(cmd, ct);

            // Só o SQL vai para o log: valores de parâmetros podem conter dados pessoais.
            _logger.LogDebug("[{Provider}] {Sql} ({ElapsedMs:0.0} ms)",
                Provider, sql, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            return result;
        }
        catch (DbException ex)
        {
            OrmException error = _dialect.Translate(ex);
            _logger.LogWarning(ex, "[{Provider}] {Kind} ao executar: {Sql}", Provider, error.Kind, sql);
            throw error;
        }
        finally
        {
            if (ownConnection && connection is not null)
                await connection.DisposeAsync();   // devolve ao pool imediatamente

            Volatile.Write(ref _operationInProgress, 0);
        }
    }

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken ct)
    {
        DbConnection connection = _dialect.CreateConnection(_options.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch (DbException ex)
        {
            await connection.DisposeAsync();
            throw _dialect.Translate(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_transaction is not null)
        {
            // Chegou ao fim da requisição com transação aberta = esqueceram o Commit.
            // Rollback é o único comportamento seguro.
            _logger.LogWarning("[{Provider}] Sessão descartada com transação aberta. Fazendo ROLLBACK.", Provider);

            try { await EndTransactionAsync(commit: false, CancellationToken.None); }
            catch (OrmException ex) { _logger.LogError(ex, "[{Provider}] Falha no ROLLBACK automático.", Provider); }
        }
    }
}
