namespace MeuOrm.Models;

public class Cliente
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Ativo { get; set; } = true;
}
