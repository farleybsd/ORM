package meuorm;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

public class Main {

    public static void main(String[] args) throws SQLException {
        String url = "jdbc:postgresql://localhost:5432/loja";

        try (Connection connection = DriverManager.getConnection(url, "postgres", "postgres")) {

            try (PreparedStatement ps = connection.prepareStatement(
                    "INSERT INTO cliente (nome, email) VALUES (?, ?) RETURNING id")) {
                ps.setString(1, "Maria");
                // No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
                ps.setString(2, "maria-" + UUID.randomUUID() + "@email.com");

                try (ResultSet rs = ps.executeQuery()) {
                    rs.next();
                    System.out.println("Cliente criado com id " + rs.getLong("id"));
                }
            }

            // Um SELECT na mão
            try (PreparedStatement ps = connection.prepareStatement("SELECT id, nome, email FROM cliente");
                 ResultSet rs = ps.executeQuery()) {
                while (rs.next()) {
                    System.out.println(rs.getLong("id") + " - " + rs.getString("nome") + " - " + rs.getString("email"));
                }
            }
        }
    }
}
