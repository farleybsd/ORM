namespace Sinistros.Api.Features.ExcluirSinistro;

public sealed class Request
{
    /// <summary>Vem da rota.</summary>
    public Guid Id { get; set; }

    /// <summary>Vem da query string (?versao=N): versão lida na consulta.</summary>
    public int Versao { get; set; }
}
