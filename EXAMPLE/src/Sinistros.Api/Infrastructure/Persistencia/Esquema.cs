namespace Sinistros.Api.Infrastructure.Persistencia;

// Nomes do esquema em um só lugar (container/*/init/01-schema.sql). Os nomes das colunas
// também são usados como nomes dos parâmetros (@id, @apolice_id...).

internal static class TabelaSinistro
{
    public const string Nome = "sinistro";

    public const string Id = "id";
    public const string ApoliceId = "apolice_id";
    public const string DataOcorrencia = "data_ocorrencia";
    public const string TipoCobertura = "tipo_cobertura";
    public const string Descricao = "descricao";
    public const string Status = "status";
    public const string DataRegistro = "data_registro";
    public const string Versao = "versao";
    public const string AtualizadoEm = "atualizado_em";
}
