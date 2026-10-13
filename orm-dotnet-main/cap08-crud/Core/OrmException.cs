namespace MeuOrm.Core;

public enum OrmErrorKind
{
    Other,          // qualquer erro não classificado
    Connection,     // não conseguiu falar com o banco
    Duplicate,      // violou UNIQUE (ex.: e-mail repetido)
    ForeignKey,     // violou chave estrangeira
    NotNull,        // campo obrigatório veio nulo
    TableNotFound,
    ColumnNotFound,
    Deadlock,
    Concurrency     // outro usuário alterou o registro antes (capítulo 14)
}

public class OrmException : Exception
{
    public OrmErrorKind Kind { get; }
    public string Detail { get; }   // nome da constraint ou da coluna, quando houver

    public OrmException(OrmErrorKind kind, string message,
                        string detail = "", Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Detail = detail;
    }
}
