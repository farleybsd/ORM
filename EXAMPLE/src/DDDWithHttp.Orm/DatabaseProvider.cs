namespace DDDWithHttp.Orm;

/// <summary>
/// Bancos suportados pelo ORM. É também a chave de cada conexão configurada em
/// <see cref="Configuration.OrmOptions.Connections"/>: uma aplicação pode ter as duas ao mesmo tempo.
/// </summary>
public enum DatabaseProvider
{
    Postgres,
    SqlServer
}
