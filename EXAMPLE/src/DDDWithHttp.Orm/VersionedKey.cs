namespace DDDWithHttp.Orm;

/// <summary>
/// Identifica a linha de uma operação com trava otimista: tabela, chave e a versão que o cliente leu.
/// </summary>
/// <param name="Table">Nome da tabela.</param>
/// <param name="KeyColumn">Coluna da chave primária.</param>
/// <param name="KeyValue">Valor da chave.</param>
/// <param name="ExpectedVersion">Versão lida pelo cliente; a operação só acontece se o banco ainda estiver nela.</param>
/// <param name="VersionColumn">Coluna que guarda a versão.</param>
public sealed record VersionedKey(
    string Table,
    string KeyColumn,
    object KeyValue,
    int ExpectedVersion,
    string VersionColumn = VersionedKey.DefaultVersionColumn)
{
    public const string DefaultVersionColumn = "versao";
}
