namespace My_Orm_postgres.Api.Features.CriarCliente;

public sealed class Request
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
