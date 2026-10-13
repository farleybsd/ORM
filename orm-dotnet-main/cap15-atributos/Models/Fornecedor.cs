namespace MeuOrm.Models;

[Table("tb_fornecedor_legado")]
public class Fornecedor : Model
{
    public string RazaoSocial { get; set; } = "";
}
