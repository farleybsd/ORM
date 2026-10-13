using System.Data;

namespace MeuOrm.Core;

public class Orm : IOrm
{
    private readonly IOrm driver;

    public Orm()
    {
        string name = DotEnv.Get("ORM_DRIVER");               // ex.: "Postgres"
        Type? type = Type.GetType($"MeuOrm.Drivers.{name}");

        if (type == null)
            throw new InvalidOperationException(
                $"Driver '{name}' não encontrado. Confira ORM_DRIVER no .env " +
                "(maiúsculas e minúsculas fazem diferença: Postgres, não postgres).");

        if (!typeof(IOrm).IsAssignableFrom(type))
            throw new InvalidOperationException(
                $"A classe '{name}' existe, mas não implementa IOrm.");

        driver = (IOrm)Activator.CreateInstance(type)!;
    }

    // Daqui para baixo: cada método só repassa para o driver
    public void Exec(string sql, string action, Dictionary<string, object?>? args = null)
        => driver.Exec(sql, action, args);

    public DataTable Select(string table, string fields = "*", string where = "",
                            Dictionary<string, object?>? args = null, string order = "")
        => driver.Select(table, fields, where, args, order);

    public void Insert(string table, Dictionary<string, object?> data)
        => driver.Insert(table, data);

    public void Update(string table, Dictionary<string, object?> data, string where,
                       Dictionary<string, object?>? args = null)
        => driver.Update(table, data, where, args);

    public void Delete(string table, string where, Dictionary<string, object?>? args = null)
        => driver.Delete(table, where, args);

    public void BeginTransaction() => driver.BeginTransaction();
    public void Commit() => driver.Commit();
    public void Rollback() => driver.Rollback();

    public string GetStatus() => driver.GetStatus();
    public string GetDbError() => driver.GetDbError();
    public long GetRowsAffected() => driver.GetRowsAffected();
    public long GetGeneratedId() => driver.GetGeneratedId();
    public DataTable GetDataTable() => driver.GetDataTable();
    public string GetLastSqlSentence() => driver.GetLastSqlSentence();

    public void Dispose() => driver.Dispose();
}
