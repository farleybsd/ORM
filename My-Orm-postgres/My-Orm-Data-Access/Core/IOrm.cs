using System.Data;

namespace My_Orm_Data_Access.Core;

// Cada operação existe em duas versões: síncrona e Async (com CancellationToken)
public interface IOrm : IDisposable, IAsyncDisposable
{
    // SQL livre: Query devolve as linhas, Execute devolve quantas linhas foram afetadas
    DataTable Query(string sql, Dictionary<string, object?>? args = null);
    Task<DataTable> QueryAsync(string sql, Dictionary<string, object?>? args = null,
                               CancellationToken ct = default);

    // SQL livre direto para objetos: T pode ser um Model, um DTO/record ou um tipo simples (long, string...)
    List<T> Query<T>(string sql, Dictionary<string, object?>? args = null);
    Task<List<T>> QueryAsync<T>(string sql, Dictionary<string, object?>? args = null,
                                CancellationToken ct = default);

    // Só a primeira linha (default/null se não vier nenhuma). Use LIMIT 1 no SQL quando fizer sentido
    T? QueryFirst<T>(string sql, Dictionary<string, object?>? args = null);
    Task<T?> QueryFirstAsync<T>(string sql, Dictionary<string, object?>? args = null,
                                CancellationToken ct = default);

    long Execute(string sql, Dictionary<string, object?>? args = null);
    Task<long> ExecuteAsync(string sql, Dictionary<string, object?>? args = null,
                            CancellationToken ct = default);

    // Atalhos de CRUD (montam o SQL e devolvem o resultado na hora)
    DataTable Select(string table, string fields = "*", string where = "",
                     Dictionary<string, object?>? args = null, string order = "");
    Task<DataTable> SelectAsync(string table, string fields = "*", string where = "",
                                Dictionary<string, object?>? args = null, string order = "",
                                CancellationToken ct = default);

    // id gerado (0 se a tabela não tem "id")
    long Insert(string table, Dictionary<string, object?> data);
    Task<long> InsertAsync(string table, Dictionary<string, object?> data,
                           CancellationToken ct = default);

    // linhas afetadas
    long Update(string table, Dictionary<string, object?> data, string where,
                Dictionary<string, object?>? args = null);
    Task<long> UpdateAsync(string table, Dictionary<string, object?> data, string where,
                           Dictionary<string, object?>? args = null, CancellationToken ct = default);

    // linhas afetadas
    long Delete(string table, string where, Dictionary<string, object?>? args = null);
    Task<long> DeleteAsync(string table, string where, Dictionary<string, object?>? args = null,
                           CancellationToken ct = default);

    // Transação manual
    void BeginTransaction();
    Task BeginTransactionAsync(CancellationToken ct = default);
    void Commit();
    Task CommitAsync(CancellationToken ct = default);
    void Rollback();
    Task RollbackAsync(CancellationToken ct = default);
}
