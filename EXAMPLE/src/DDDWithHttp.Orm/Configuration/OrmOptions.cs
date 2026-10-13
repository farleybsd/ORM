namespace DDDWithHttp.Orm.Configuration;

/// <summary>
/// Configuração do ORM, lida da seção <see cref="SectionName"/> do appsettings:
/// <code>
/// "Orm": {
///   "Connections": {
///     "Postgres":  { "ConnectionString": "Host=...;Database=..." },
///     "SqlServer": { "ConnectionString": "Server=...;Database=..." }
///   }
/// }
/// </code>
/// Cada banco configurado vira uma conexão independente, e a mesma aplicação pode usar todas.
/// </summary>
public sealed class OrmOptions
{
    public const string SectionName = "Orm";

    public Dictionary<DatabaseProvider, OrmConnectionOptions> Connections { get; set; } = new();

    /// <summary>Quantas vezes uma transação é tentada quando o banco a aborta por deadlock/serialização.</summary>
    public int MaxTransactionAttempts { get; set; } = 3;
}

public sealed class OrmConnectionOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Tempo máximo de cada comando. Evita requisições presas segurando conexão e locks.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
