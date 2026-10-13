package meuorm;

import java.util.UUID;
import meuorm.core.DotEnv;
import meuorm.models.Cliente;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        Cliente nova = new Cliente();
        nova.setNome("Maria");
        nova.setEmail("maria-" + UUID.randomUUID() + "@email.com");
        nova.save();

        Cliente c = new Cliente();
        c.load(nova.getId());

        c.setNome("Maria Souza");
        System.out.println("Gravou? " + c.save());           // true: só nome e updated_at vão no UPDATE

        boolean gravou = c.save();
        System.out.println("Gravou de novo? " + gravou);     // false: nada mudou desde o último save
    }
}
