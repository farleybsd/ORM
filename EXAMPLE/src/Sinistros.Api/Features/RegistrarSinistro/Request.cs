namespace Sinistros.Api.Features.RegistrarSinistro;

public sealed class Request
{
    public Guid ApoliceId { get; set; }
    public DateOnly DataOcorrencia { get; set; }
    public string TipoCobertura { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
