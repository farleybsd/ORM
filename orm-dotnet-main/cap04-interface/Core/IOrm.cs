using System.Data;

namespace MeuOrm.Core;

public interface IOrm : IDisposable
{
    // A porta única: todo SQL passa por aqui
    void Exec(string sql, string action, Dictionary<string, object?>? args = null);

    // Atalhos de CRUD (montam o SQL e chamam o Exec)
    DataTable Select(string table, string fields = "*", string where = "",
                     Dictionary<string, object?>? args = null, string order = "");
    void Insert(string table, Dictionary<string, object?> data);
    void Update(string table, Dictionary<string, object?> data, string where,
                Dictionary<string, object?>? args = null);
    void Delete(string table, string where, Dictionary<string, object?>? args = null);

    // Transação manual
    void BeginTransaction();
    void Commit();
    void Rollback();

    // Estado da última operação
    string GetStatus();
    string GetDbError();
    long GetRowsAffected();
    long GetGeneratedId();
    DataTable GetDataTable();
    string GetLastSqlSentence();
}
