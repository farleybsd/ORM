package meuorm;

import java.util.List;
import java.util.Map;
import java.util.UUID;
import meuorm.core.DotEnv;
import meuorm.core.Orm;
import meuorm.core.Params;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        try (Orm orm = new Orm()) {
            // No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
            orm.insert("cliente", Params.of("nome", "João", "email", "joao-" + UUID.randomUUID() + "@email.com"));
            long id = orm.getGeneratedId();
            System.out.println("Novo id: " + id);

            orm.update("cliente", Params.of("nome", "João Pedro"), "id = @id", Params.of("id", id));
            System.out.println(orm.getRowsAffected() + " linha(s) alterada(s)");

            List<Map<String, Object>> rows = orm.select("cliente", "nome", "id = @id", Params.of("id", id));
            System.out.println(rows.get(0).get("nome"));   // João Pedro

            orm.delete("cliente", "id = @id", Params.of("id", id));
            System.out.println(orm.getLastSqlSentence());   // DELETE FROM cliente WHERE id = @id

            // A trava do WHERE
            try {
                orm.update("cliente", Params.of("nome", "Todo mundo"), "", null);
            } catch (IllegalArgumentException ex) {
                System.out.println(ex.getMessage());
            }
        }
    }
}
