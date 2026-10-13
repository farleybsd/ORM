namespace MeuOrm.Models;

public class Cliente : Model
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Ativo { get; set; } = true;
}
