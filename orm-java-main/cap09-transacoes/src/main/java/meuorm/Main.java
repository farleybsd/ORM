package meuorm;

import java.math.BigDecimal;
import java.util.UUID;

import meuorm.core.DotEnv;
import meuorm.core.Orm;
import meuorm.core.OrmException;
import meuorm.core.Params;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        try (Orm orm = new Orm()) {
            // Um cliente para ser dono dos pedidos
            orm.insert("cliente", Params.of("nome", "Cliente do pedido", "email", "pedido-" + UUID.randomUUID() + "@email.com"));
            long clienteId = orm.getGeneratedId();

            gravarPedido(orm, clienteId, "Monitor");   // tudo certo: grava
            gravarPedido(orm, clienteId, null);        // teste do rollback: nada é gravado
        }
    }

    static void gravarPedido(Orm orm, long clienteId, String segundoProduto) {
        orm.beginTransaction();
        try {
            orm.insert("pedido", Params.of("cliente_id", clienteId, "total", new BigDecimal("300.00")));
            long pedidoId = orm.getGeneratedId();

            orm.insert("pedido_item", Params.of("pedido_id", pedidoId, "produto", "Teclado", "valor", new BigDecimal("100.00")));
            orm.insert("pedido_item", Params.of("pedido_id", pedidoId, "produto", segundoProduto, "valor", new BigDecimal("200.00")));

            orm.commit();
            System.out.println("Pedido " + pedidoId + " gravado.");
        } catch (OrmException ex) {
            orm.rollback();
            System.out.println("Nada foi gravado: " + ex.getMessage());
        }
    }
}
