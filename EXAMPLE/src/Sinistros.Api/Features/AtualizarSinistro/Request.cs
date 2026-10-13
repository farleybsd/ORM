namespace Sinistros.Api.Features.AtualizarSinistro;

public sealed class Request
{
    /// <summary>Vem da rota.</summary>
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    /// <summary>Versão lida no GET. Se outra requisição gravou depois, a atualização é recusada (409).</summary>
    public int Versao { get; set; }
}
