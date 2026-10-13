using Apolices.Api.Contracts;

namespace Apolices.Api.Features.AtualizarApolice;

public sealed class Request : DadosApoliceRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid Id { get; set; }

    /// <summary>Versão lida no GET. Se outra requisição gravou depois, a atualização é recusada (409).</summary>
    public int Versao { get; set; }
}
