namespace Sinistros.Api.Domain;

/// <summary>
/// Vocabulário próprio do contexto de Sinistros. Não é compartilhado com o contexto de Apólices;
/// a tradução entre os dois vocabulários acontece na camada de infraestrutura (cliente HTTP).
/// </summary>
public enum TipoCobertura
{
    Colisao,
    Roubo,
    Incendio,
    DanosTerceiros
}
