package meuorm.drivers;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;

import org.postgresql.util.PSQLException;
import org.postgresql.util.ServerErrorMessage;

import meuorm.core.DotEnv;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;

public class Postgres extends DriverBase {

    private final String url;
    private final String user;
    private final String password;

    public Postgres() {
        url = "jdbc:postgresql://" + DotEnv.get("DB_HOST") + ":" + DotEnv.get("DB_PORT")
                + "/" + DotEnv.get("DB_NAME");
        user = DotEnv.get("DB_USER");
        password = DotEnv.get("DB_PASSWORD");
    }

    @Override
    protected Connection createConnection() throws SQLException {
        return DriverManager.getConnection(url, user, password);
    }

    @Override
    protected OrmException translateError(SQLException ex) {
        String state = ex.getSQLState() == null ? "" : ex.getSQLState();

        // Classe 08 = conexão; 28 = autenticação; sem código = nem chegou ao servidor
        if (state.isEmpty() || state.startsWith("08") || state.startsWith("28")) {
            return new OrmException(OrmErrorKind.CONNECTION,
                    "Falha de conexão com o banco: " + ex.getMessage(), "", ex);
        }

        ServerErrorMessage server = ex instanceof PSQLException p ? p.getServerErrorMessage() : null;
        String constraint = server != null && server.getConstraint() != null ? server.getConstraint() : "";
        String column = server != null && server.getColumn() != null ? server.getColumn() : "";
        String text = server != null && server.getMessage() != null ? server.getMessage() : ex.getMessage();

        return switch (state) {
            case "23505" -> new OrmException(OrmErrorKind.DUPLICATE,
                    "Registro duplicado (" + constraint + ")", constraint, ex);
            case "23503" -> new OrmException(OrmErrorKind.FOREIGN_KEY,
                    "Referência inválida (" + constraint + ")", constraint, ex);
            case "23502" -> new OrmException(OrmErrorKind.NOT_NULL,
                    "Campo obrigatório não informado: " + column, column, ex);
            case "42P01" -> new OrmException(OrmErrorKind.TABLE_NOT_FOUND, text, "", ex);
            case "42703" -> new OrmException(OrmErrorKind.COLUMN_NOT_FOUND, text, "", ex);
            case "40P01" -> new OrmException(OrmErrorKind.DEADLOCK,
                    "Deadlock detectado. Tente a operação novamente.", "", ex);
            default -> new OrmException(OrmErrorKind.OTHER, text, "", ex);
        };
    }
}
