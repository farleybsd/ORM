namespace Apolices.Api.Infrastructure.Persistencia;

// Nomes do esquema em um só lugar (container/*/init/01-schema.sql). Os nomes das colunas
// também são usados como nomes dos parâmetros (@id, @numero...).

internal static class TabelaApolice
{
    public const string Nome = "apolice";

    public const string Id = "id";
    public const string Numero = "numero";
    public const string SeguradoNome = "segurado_nome";
    public const string SeguradoDocumento = "segurado_documento";
    public const string VigenciaInicio = "vigencia_inicio";
    public const string VigenciaFim = "vigencia_fim";
    public const string Versao = "versao";
    public const string CriadoEm = "criado_em";
    public const string AtualizadoEm = "atualizado_em";

    public const string UqNumero = "uq_apolice_numero";
}

internal static class TabelaCobertura
{
    public const string Nome = "apolice_cobertura";

    public const string ApoliceId = "apolice_id";
    public const string Tipo = "tipo";
    public const string LimiteIndenizacao = "limite_indenizacao";
}
