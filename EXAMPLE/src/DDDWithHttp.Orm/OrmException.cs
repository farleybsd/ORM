namespace DDDWithHttp.Orm;

public enum OrmErrorKind
{
    Other,                  // qualquer erro não classificado
    Connection,             // não conseguiu falar com o banco
    Timeout,                // comando passou do CommandTimeout
    Duplicate,              // violou UNIQUE / PRIMARY KEY
    ForeignKey,             // violou chave estrangeira
    NotNull,                // campo obrigatório veio nulo
    CheckViolation,         // violou CHECK constraint
    TableNotFound,
    ColumnNotFound,
    Deadlock,               // o banco escolheu esta transação como vítima
    SerializationFailure,   // conflito em isolamento Serializable/RepeatableRead
    Concurrency             // trava otimista: outro usuário alterou o registro antes
}

/// <summary>
/// Erro de banco já traduzido para um vocabulário único, independente do provedor.
/// O código de quem usa o ORM nunca precisa conhecer SqlState do Postgres nem números do SQL Server.
/// </summary>
public sealed class OrmException : Exception
{
    public OrmErrorKind Kind { get; }

    /// <summary>Nome da constraint ou da coluna envolvida, quando o banco informa.</summary>
    public string Detail { get; }

    public OrmException(OrmErrorKind kind, string message, string? detail = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Detail = detail ?? string.Empty;
    }

    /// <summary>
    /// Erros em que o banco GARANTE que a transação inteira foi desfeita, então repetir é seguro.
    /// Falha de conexão não entra: se ela acontecer durante o COMMIT, não dá para saber se gravou.
    /// </summary>
    public bool IsTransient => Kind is OrmErrorKind.Deadlock or OrmErrorKind.SerializationFailure;
}
