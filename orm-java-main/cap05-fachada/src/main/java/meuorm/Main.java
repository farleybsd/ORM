package meuorm;

import meuorm.core.DotEnv;
import meuorm.core.Orm;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        // Ainda não existe nenhum driver. Rodando agora, a fachada procura
        // a classe meuorm.drivers.Postgres e explica o que está faltando.
        try (Orm orm = new Orm()) {
            System.out.println("Driver carregado: " + orm.getStatus());
        } catch (IllegalStateException ex) {
            System.out.println(ex.getMessage());
        }
    }
}
