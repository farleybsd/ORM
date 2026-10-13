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
            String email = "ana-" + UUID.randomUUID() + "@email.com";
            orm.insert("cliente", Params.of("nome", "Ana", "email", email));

            try {
                orm.insert("cliente", Params.of("nome", "Ana de novo", "email", email));
            } catch (OrmException ex) {
                switch (ex.getKind()) {
                    case DUPLICATE -> System.out.println("Este e-mail já está cadastrado.");
                    case CONNECTION -> System.out.println("Banco fora do ar. Tente em alguns minutos.");
                    default -> throw ex;
                }
            }

            // Os outros tipos de erro, um de cada
            testar("Chave estrangeira", () -> orm.insert("pedido", Params.of("cliente_id", -1L, "total", BigDecimal.ONE)));
            testar("Campo obrigatório", () -> orm.insert("cliente", Params.of("nome", null, "email", "x@x.com")));
            testar("Tabela inexistente", () -> orm.exec("SELECT * FROM nao_existe", "query"));
            testar("Coluna inexistente", () -> orm.exec("SELECT coluna_x FROM cliente", "query"));
        }
    }

    static void testar(String titulo, Runnable acao) {
        try {
            acao.run();
        } catch (OrmException ex) {
            System.out.println(titulo + ": kind=" + ex.getKind() + " | " + ex.getMessage());
        }
    }
}
