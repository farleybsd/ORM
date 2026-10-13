package meuorm;

import java.util.UUID;
import meuorm.core.DotEnv;
import meuorm.models.Cliente;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        // No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
        Cliente novo = new Cliente();
        novo.setNome("Carla");
        novo.setEmail("carla-" + UUID.randomUUID() + "@email.com");
        novo.save();                                   // INSERT
        System.out.println("Criada com id " + novo.getId());

        Cliente carla = new Cliente();
        if (carla.load(novo.getId())) {
            carla.setNome("Carla Souza");
            carla.save();                              // UPDATE (ainda grava todas as colunas)
            System.out.println("Alterada: " + carla.getNome());
        }
    }
}
