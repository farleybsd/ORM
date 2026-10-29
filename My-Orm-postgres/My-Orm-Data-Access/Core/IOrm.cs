using System.Data;

namespace My_Orm_Data_Access.Core;

public interface IOrm : IDisposable
{
    // SQL livre: Query devolve as linhas, Execute devolve quantas linhas foram afetadas
    DataTable Query(string sql, Dictionary<string, object?>? args = null);
    long Execute(string sql, Dictionary<string, object?>? args = null);

    // Atalhos de CRUD (montam o SQL e devolvem o resultado na hora)
    DataTable Select(string table, string fields = "*", string where = "",
                     Dictionary<string, object?>? args = null, string order = "");
    long Insert(string table, Dictionary<string, object?> data);     // id gerado (0 se a tabela não tem "id")
    long Update(string table, Dictionary<string, object?> data, string where,
                Dictionary<string, object?>? args = null);           // linhas afetadas
    long Delete(string table, string where, Dictionary<string, object?>? args = null); // linhas afetadas

    // Transação manual
    void BeginTransaction();
    void Commit();
    void Rollback();
}
