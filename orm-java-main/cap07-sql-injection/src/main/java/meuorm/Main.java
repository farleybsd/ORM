package meuorm;

import java.util.List;
import meuorm.core.DotEnv;
import meuorm.core.Orm;
import meuorm.core.Params;
import meuorm.drivers.NamedSql;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        try (Orm orm = new Orm()) {
            // O que o usuário digitou no campo de e-mail:
            String email = "' OR '1'='1";

            // ERRADO: o valor colado no SQL vira comando
            orm.exec("SELECT * FROM cliente WHERE email = '" + email + "'", "query");
            System.out.println("Concatenado: " + orm.getRowsAffected() + " linha(s) (devolveu TODOS os clientes)");

            // CERTO: o valor viaja como parâmetro
            orm.exec("SELECT * FROM cliente WHERE email = @email", "query", Params.of("email", email));
            System.out.println("Com parâmetro: " + orm.getRowsAffected() + " linha(s)");

            // LIKE: o % vai dentro do valor
            String texto = "ari";
            orm.exec("SELECT * FROM cliente WHERE nome ILIKE @busca", "query", Params.of("busca", "%" + texto + "%"));
            System.out.println("LIKE '%" + texto + "%': " + orm.getRowsAffected() + " linha(s)");

            // O que o NamedSql entrega ao JDBC
            NamedSql named = NamedSql.parse("SELECT * FROM cliente WHERE email = @email AND nome <> 'a@b'");
            System.out.println("SQL para o JDBC: " + named.sql() + " | parâmetros: " + named.names());

            // Ordenação escolhida pelo usuário: compare com uma lista fixa
            String escolhaDoUsuario = "email; DROP TABLE cliente";
            List<String> permitidas = List.of("nome", "email", "inserted_at");
            String order = permitidas.contains(escolhaDoUsuario) ? escolhaDoUsuario : "nome";
            System.out.println("Ordenação usada: " + order);
        }
    }
}
