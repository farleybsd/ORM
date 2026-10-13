package meuorm;

import java.util.List;
import java.util.Map;

import meuorm.core.DotEnv;
import meuorm.core.Orm;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;
import meuorm.models.Cliente;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();
        System.out.println("Driver: " + DotEnv.get("ORM_DRIVER") + "\n");

        // 1. Criar
        Cliente maria = new Cliente();
        maria.setNome("Maria");
        maria.setEmail("maria" + System.nanoTime() + "@email.com");
        maria.save();
        System.out.println("1. Criada: id " + maria.getId());

        // 2. Duplicado: o banco recusa e o ORM traduz
        try {
            Cliente outra = new Cliente();
            outra.setNome("Outra Maria");
            outra.setEmail(maria.getEmail());
            outra.save();
        } catch (OrmException ex) {
            if (ex.getKind() != OrmErrorKind.DUPLICATE) {
                throw ex;
            }
            System.out.println("2. E-mail repetido recusado, como esperado");
        }

        // 3. Ler e alterar só um campo (dirty-check)
        Cliente c = new Cliente();
        c.load(maria.getId());
        c.setNome("Maria Souza");
        c.save();
        System.out.println("3. Alterada: " + c.getNome());

        // 4. Save sem mudança não vai ao banco
        System.out.println("4. Gravou de novo? " + c.save());

        // 5. Conflito: dois usuários editando ao mesmo tempo
        Cliente ana = new Cliente();
        ana.load(maria.getId());
        Cliente bruno = new Cliente();
        bruno.load(maria.getId());

        ana.setAtivo(false);
        ana.save();

        bruno.setNome("Nome do Bruno");
        try {
            bruno.save();
        } catch (OrmException ex) {
            if (ex.getKind() != OrmErrorKind.CONCURRENCY) {
                throw ex;
            }
            System.out.println("5. Conflito detectado: Bruno precisa recarregar");
        }

        // 6. Listar com SQL visível
        try (Orm orm = new Orm()) {
            List<Map<String, Object>> rows = orm.select("cliente", "id, nome, ativo", "", null, "id DESC");
            System.out.println("6. " + rows.size() + " cliente(s). SQL: " + orm.getLastSqlSentence());

            for (Map<String, Object> row : rows.subList(0, Math.min(5, rows.size()))) {
                System.out.printf("   %4s | %-20s | ativo: %s%n", row.get("id"), row.get("nome"), row.get("ativo"));
            }
        }

        // 7. Apagar
        c.load(maria.getId());
        c.delete();
        System.out.println("7. Apagada. Ainda existe? " + new Cliente().load(maria.getId()));
    }
}
