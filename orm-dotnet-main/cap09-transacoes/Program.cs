using MeuOrm.Core;

DotEnv.Load();
using var orm = new Orm();

// Um cliente para ser dono dos pedidos
orm.Insert("cliente", new() { ["nome"] = "Cliente do pedido", ["email"] = $"pedido-{Guid.NewGuid():N}@email.com" });
long clienteId = orm.GetGeneratedId();

GravarPedido(orm, clienteId, "Monitor");   // tudo certo: grava
GravarPedido(orm, clienteId, null);        // teste do rollback: nada é gravado

static void GravarPedido(Orm orm, long clienteId, string? segundoProduto)
{
    orm.BeginTransaction();
    try
    {
        orm.Insert("pedido", new() { ["cliente_id"] = clienteId, ["total"] = 300m });
        long pedidoId = orm.GetGeneratedId();

        orm.Insert("pedido_item", new() { ["pedido_id"] = pedidoId, ["produto"] = "Teclado", ["valor"] = 100m });
        orm.Insert("pedido_item", new() { ["pedido_id"] = pedidoId, ["produto"] = segundoProduto, ["valor"] = 200m });

        orm.Commit();
        Console.WriteLine($"Pedido {pedidoId} gravado.");
    }
    catch (OrmException ex)
    {
        orm.Rollback();
        Console.WriteLine($"Nada foi gravado: {ex.Message}");
    }
}
