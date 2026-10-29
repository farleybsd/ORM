namespace My_Orm_postgres.Api.Features.AtualizarCliente;

public sealed class Request
{
    public long Id { get; set; }          // vem da rota /clientes/{id}
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public long Versao { get; set; }      // a "versao" recebida na consulta (concorrência otimista)
}
