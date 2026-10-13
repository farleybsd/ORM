using System.Data.Common;

namespace DDDWithHttp.Orm.Dialects;

/// <summary>
/// Tudo que muda de um banco para outro fica atrás desta interface (padrão Strategy, como os
/// drivers do capítulo 16). O resto do ORM — sessão, transação, CRUD — é igual para todos.
/// </summary>
internal interface ISqlDialect
{
    DatabaseProvider Provider { get; }

    DbConnection CreateConnection(string connectionString);

    /// <summary>Converte o valor .NET para o formato que o driver espera (enum, DateOnly, DateTime UTC...).</summary>
    void BindValue(DbParameter parameter, object? value);

    /// <summary>Traduz o erro nativo do driver para o vocabulário do ORM.</summary>
    OrmException Translate(DbException exception);
}
