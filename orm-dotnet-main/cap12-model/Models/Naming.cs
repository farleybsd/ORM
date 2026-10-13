using System.Text;

namespace MeuOrm.Models;

public static class Naming
{
    // "PedidoItem" -> "pedido_item"
    public static string ToSnake(string name)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');          // maiúscula no meio = começa palavra nova

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
