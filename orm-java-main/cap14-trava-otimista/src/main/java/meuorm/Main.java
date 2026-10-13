package meuorm;

import java.util.UUID;
import meuorm.core.DotEnv;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;
import meuorm.models.Cliente;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        Cliente registro = new Cliente();
        registro.setNome("Cliente");
        registro.setEmail("conflito-" + UUID.randomUUID() + "@email.com");
        registro.save();

        Cliente ana = new Cliente();
        ana.load(registro.getId());

        Cliente bruno = new Cliente();
        bruno.load(registro.getId());     // os dois leram a mesma versão

        ana.setNome("Nome da Ana");
        ana.save();                       // grava e muda o updated_at
        System.out.println("Ana gravou.");

        bruno.setNome("Nome do Bruno");
        try {
            bruno.save();                 // condição falha: zero linhas
        } catch (OrmException ex) {
            if (ex.getKind() != OrmErrorKind.CONCURRENCY) {
                throw ex;
            }
            System.out.println("Bruno: " + ex.getMessage());
        }
    }
}
