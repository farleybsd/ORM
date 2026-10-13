using System.Data;

namespace DDDWithHttp.Orm;

/// <summary>
/// Unidade de trabalho com UM banco (equivalente ao IOrm do curso, em versão assíncrona).
/// <para>
/// Regras de uso:
/// <list type="bullet">
/// <item>Não é thread-safe: uma sessão por requisição (scoped), uma operação por vez — como o DbContext do EF.</item>
/// <item>Fora de transação, cada comando pega uma conexão do pool e devolve logo em seguida ("abrir tarde, fechar cedo").</item>
/// <item>Dentro de transação, todos os comandos usam a mesma conexão até o Commit/Rollback.</item>
/// <item>Todo valor entra como parâmetro (@nome). Nunca concatene valor no SQL.</item>
/// </list>
/// </para>
/// </summary>
public interface IDbSession : IAsyncDisposable
{
    DatabaseProvider Provider { get; }

    bool HasActiveTransaction { get; }

    // ===== SQL livre (a "porta única") =====
    Task<IReadOnlyList<DbRow>> QueryAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default);
    Task<DbRow?> QuerySingleOrDefaultAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default);
    Task<int> ExecuteAsync(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default);
    Task<T?> ExecuteScalarAsync<T>(string sql, Dictionary<string, object?>? args = null, CancellationToken ct = default);

    // ===== Atalhos de CRUD (montam o SQL a partir de nomes de tabela/coluna validados) =====
    Task<int> InsertAsync(string table, Dictionary<string, object?> data, CancellationToken ct = default);
    Task<int> UpdateAsync(string table, Dictionary<string, object?> data, string where,
                          Dictionary<string, object?>? args = null, CancellationToken ct = default);
    Task<int> DeleteAsync(string table, string where, Dictionary<string, object?>? args = null, CancellationToken ct = default);

    /// <summary>
    /// UPDATE com trava otimista: só altera se a versão no banco for <paramref name="expectedVersion"/>
    /// e incrementa a versão. Lança <see cref="OrmException"/> com <see cref="OrmErrorKind.Concurrency"/>
    /// se outro usuário alterou/excluiu antes. Retorna a nova versão.
    /// </summary>
    Task<int> UpdateVersionedAsync(VersionedKey key, Dictionary<string, object?> data, CancellationToken ct = default);

    /// <summary>DELETE com trava otimista (mesma regra de <see cref="UpdateVersionedAsync"/>).</summary>
    Task DeleteVersionedAsync(VersionedKey key, CancellationToken ct = default);

    // ===== Transação =====

    /// <summary>
    /// Abre uma transação explícita. Use com <c>await using</c>: se o Commit não for chamado,
    /// o Dispose faz Rollback — nenhum caminho de erro deixa transação pendurada.
    /// </summary>
    Task<IOrmTransaction> BeginTransactionAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default);

    /// <summary>
    /// Executa <paramref name="work"/> dentro de uma transação: Commit se terminar bem, Rollback se lançar exceção.
    /// <list type="bullet">
    /// <item>Se já existe transação aberta nesta sessão, <paramref name="work"/> participa dela (não abre outra).
    ///   Assim repositórios são atômicos sozinhos e o caso de uso pode ampliar a fronteira quando precisar.</item>
    /// <item>Deadlock/conflito de serialização: o banco já desfez tudo, então a transação inteira é repetida
    ///   (até <c>MaxTransactionAttempts</c>). Por isso <paramref name="work"/> não deve ter efeitos fora do banco.</item>
    /// </list>
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work,
                                         IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work,
                                   IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default);
}
