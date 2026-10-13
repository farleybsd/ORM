namespace DDDWithHttp.Orm.Sessions;

/// <summary>
/// Cria sessões novas para qualquer banco configurado. Singleton.
/// Use quando precisar de uma sessão fora do escopo da requisição (jobs, health checks).
/// Quem cria é responsável pelo <c>await using</c>.
/// </summary>
public interface IDbSessionFactory
{
    IReadOnlyCollection<DatabaseProvider> ConfiguredProviders { get; }

    bool IsConfigured(DatabaseProvider provider);

    IDbSession Create(DatabaseProvider provider);
}
