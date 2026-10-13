namespace Apolices.Api.Infrastructure.Persistencia;

/// <summary>
/// SQL do agregado Apólice. Escrito em SQL padrão (ANSI) para rodar igual no Postgres e no
/// SQL Server — as diferenças entre os bancos ficam no dialeto do ORM, não aqui.
/// </summary>
internal static class ApoliceSql
{
    // Um único SELECT com JOIN traz a apólice e as coberturas: uma ida ao banco e uma
    // fotografia consistente do agregado (evita N+1 e leituras de momentos diferentes).
    private const string SelectAgregado =
        $"SELECT a.{TabelaApolice.Id}, a.{TabelaApolice.Numero}, a.{TabelaApolice.SeguradoNome}, " +
        $"a.{TabelaApolice.SeguradoDocumento}, a.{TabelaApolice.VigenciaInicio}, a.{TabelaApolice.VigenciaFim}, " +
        $"a.{TabelaApolice.Versao}, c.{TabelaCobertura.Tipo}, c.{TabelaCobertura.LimiteIndenizacao} " +
        $"FROM {TabelaApolice.Nome} a " +
        $"LEFT JOIN {TabelaCobertura.Nome} c ON c.{TabelaCobertura.ApoliceId} = a.{TabelaApolice.Id}";

    public const string ObterPorId =
        SelectAgregado +
        $" WHERE a.{TabelaApolice.Id} = @{TabelaApolice.Id}" +
        $" ORDER BY c.{TabelaCobertura.Tipo}";

    public const string ObterTodas =
        SelectAgregado +
        $" ORDER BY a.{TabelaApolice.Numero}, c.{TabelaCobertura.Tipo}";

    public const string FiltroPorId = $"{TabelaApolice.Id} = @{TabelaApolice.Id}";

    public const string FiltroCoberturasDaApolice = $"{TabelaCobertura.ApoliceId} = @{TabelaCobertura.ApoliceId}";
}
