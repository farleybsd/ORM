namespace Apolices.Api.Common;

/// <summary>
/// Classe de um erro esperado, na linguagem do domínio. O domínio não conhece HTTP: a tradução
/// de cada tipo para um status code acontece em um único lugar (<c>ErroParaHttp.ParaStatusCode</c>).
/// </summary>
public enum TipoErro
{
    Validacao,
    NaoEncontrado,
    Conflito,
    RegraDeNegocio
}
