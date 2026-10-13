package meuorm.drivers;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;

import meuorm.core.DotEnv;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;

public class MySql extends DriverBase {

    private final String url;
    private final String user;
    private final String password;

    public MySql() {
        url = "jdbc:mysql://" + DotEnv.get("DB_HOST") + ":" + DotEnv.get("DB_PORT")
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

        if (state.startsWith("08") || ex.getErrorCode() == 1045) {
            return new OrmException(OrmErrorKind.CONNECTION,
                    "Falha de conexão com o banco: " + ex.getMessage(), "", ex);
        }

        return switch (ex.getErrorCode()) {
            case 1062 -> new OrmException(OrmErrorKind.DUPLICATE, "Registro duplicado", "", ex);
            case 1451, 1452 -> new OrmException(OrmErrorKind.FOREIGN_KEY, "Referência inválida", "", ex);
            case 1048 -> new OrmException(OrmErrorKind.NOT_NULL, ex.getMessage(), "", ex);
            case 1146 -> new OrmException(OrmErrorKind.TABLE_NOT_FOUND, ex.getMessage(), "", ex);
            case 1054 -> new OrmException(OrmErrorKind.COLUMN_NOT_FOUND, ex.getMessage(), "", ex);
            case 1213 -> new OrmException(OrmErrorKind.DEADLOCK, "Deadlock detectado", "", ex);
            default -> new OrmException(OrmErrorKind.OTHER, ex.getMessage(), "", ex);
        };
    }
}
