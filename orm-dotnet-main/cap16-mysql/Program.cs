using MeuOrm.Core;
using MeuOrm.Models;

DotEnv.Load();

// Um cliente com um pedido, para o campo calculado ter o que contar
var cliente = new Cliente { Nome = "Maria", Email = $"maria-{Guid.NewGuid():N}@email.com" };
cliente.Save();
using (var orm = new Orm())
    orm.Insert("pedido", new() { ["cliente_id"] = cliente.Id, ["total"] = 50m });

var c = new Cliente();
c.LoadQuery(@"
    SELECT c.*,
           (SELECT COUNT(*) FROM pedido p WHERE p.cliente_id = c.id) AS total_pedidos
      FROM cliente c
     WHERE c.id = @id", new() { ["id"] = cliente.Id });

Console.WriteLine($"{c.Nome} tem {c.TotalPedidos} pedido(s)");

c.Nome = "Novo nome";
c.Save();   // UPDATE só com nome e updated_at; total_pedidos nem aparece
Console.WriteLine($"Salvo: {c.Nome}");

// [Table]: classe Fornecedor gravando na tabela tb_fornecedor_legado
var f = new Fornecedor { RazaoSocial = "ACME Ltda" };
f.Save();
Console.WriteLine($"Fornecedor {f.Id} gravado em tb_fornecedor_legado");
