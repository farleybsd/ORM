namespace Sinistros.Api.Infrastructure.Persistencia;

/// <summary>
/// SQL do agregado Sinistro. Escrito em SQL padrão (ANSI) para rodar igual no Postgres e no
/// SQL Server — as diferenças entre os bancos ficam no dialeto do ORM, não aqui.
/// </summary>
internal static class SinistroSql
{
    // Colunas explícitas (nunca SELECT *): o contrato com o banco fica visível e uma coluna nova não quebra nada
    private const string Select =
        $"SELECT {TabelaSinistro.Id}, {TabelaSinistro.ApoliceId}, {TabelaSinistro.DataOcorrencia}, " +
        $"{TabelaSinistro.TipoCobertura}, {TabelaSinistro.Descricao}, {TabelaSinistro.Status}, " +
        $"{TabelaSinistro.DataRegistro}, {TabelaSinistro.Versao} " +
        $"FROM {TabelaSinistro.Nome}";

    public const string ObterPorId =
        Select + $" WHERE {TabelaSinistro.Id} = @{TabelaSinistro.Id}";

    // Usa o índice ix_sinistro_apolice (apolice_id, data_registro DESC)
    public const string ListarPorApolice =
        Select +
        $" WHERE {TabelaSinistro.ApoliceId} = @{TabelaSinistro.ApoliceId}" +
        $" ORDER BY {TabelaSinistro.DataRegistro} DESC";
}
