package meuorm;

import java.math.BigDecimal;
import java.util.UUID;
import meuorm.core.DotEnv;
import meuorm.core.Orm;
import meuorm.core.Params;
import meuorm.models.Cliente;
import meuorm.models.Fornecedor;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        // Um cliente com um pedido, para o campo calculado ter o que contar
        Cliente cliente = new Cliente();
        cliente.setNome("Maria");
        cliente.setEmail("maria-" + UUID.randomUUID() + "@email.com");
        cliente.save();
        try (Orm orm = new Orm()) {
            orm.insert("pedido", Params.of("cliente_id", cliente.getId(), "total", new BigDecimal("50.00")));
        }

        Cliente c = new Cliente();
        c.loadQuery("""
                SELECT c.*,
                       (SELECT COUNT(*) FROM pedido p WHERE p.cliente_id = c.id) AS total_pedidos
                  FROM cliente c
                 WHERE c.id = @id""", Params.of("id", cliente.getId()));

        System.out.println(c.getNome() + " tem " + c.getTotalPedidos() + " pedido(s)");

        c.setNome("Novo nome");
        c.save();   // UPDATE só com nome e updated_at; total_pedidos nem aparece
        System.out.println("Salvo: " + c.getNome());

        // @Table: a classe Fornecedor grava na tabela tb_fornecedor_legado
        Fornecedor f = new Fornecedor();
        f.setRazaoSocial("ACME Ltda");
        f.save();
        System.out.println("Fornecedor " + f.getId() + " gravado em tb_fornecedor_legado");
    }
}
