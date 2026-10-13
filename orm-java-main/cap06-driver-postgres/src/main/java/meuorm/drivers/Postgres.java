package meuorm.drivers;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.ResultSetMetaData;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import meuorm.core.DotEnv;
import meuorm.core.IOrm;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;

public class Postgres implements IOrm {

    private final String url;
    private final String user;
    private final String password;
    private Connection connection;
    private boolean inTransaction;

    // O "painel" da última operação
    private String dbError = "";
    private String lastSql = "";
    private long rowsAffected;
    private long generatedId;
    private List<Map<String, Object>> dataTable = new ArrayList<>();

    public Postgres() {
        url = "jdbc:postgresql://" + DotEnv.get("DB_HOST") + ":" + DotEnv.get("DB_PORT")
                + "/" + DotEnv.get("DB_NAME");
        user = DotEnv.get("DB_USER");
        password = DotEnv.get("DB_PASSWORD");
    }

    private void connect() throws SQLException {
        connection = DriverManager.getConnection(url, user, password);
    }

    private void disconnect() {
        if (connection != null) {
            try {
                connection.close();
            } catch (SQLException ignored) {
                // fechar uma conexão já caída não é problema nosso
            }
            connection = null;
        }
    }

    @Override
    public void exec(String sql, String action, Map<String, Object> args) {
        // 1. Zera o painel: cada execução começa limpa
        lastSql = sql;
        dbError = "";
        rowsAffected = 0;
        generatedId = 0;
        dataTable = new ArrayList<>();

        // 2. Sem transação manual aberta, este exec abre e fecha sua própria conexão
        boolean ownConnection = !inTransaction;

        try {
            if (ownConnection) {
                connect();
            }

            // 3. Troca cada @nome por ? e prepara o comando
            NamedSql named = NamedSql.parse(sql);
            boolean isInsert = action.equals("insert");

            try (PreparedStatement ps = isInsert
                    ? connection.prepareStatement(named.sql(), new String[] {"id"})
                    : connection.prepareStatement(named.sql())) {

                bind(ps, named, args);

                // 4. Escolhe o jeito certo de executar
                switch (action) {
                    case "query" -> {
                        try (ResultSet rs = ps.executeQuery()) {
                            dataTable = readAll(rs);
                        }
                        rowsAffected = dataTable.size();
                    }
                    case "insert" -> {
                        rowsAffected = ps.executeUpdate();
                        try (ResultSet keys = ps.getGeneratedKeys()) {
                            if (keys.next()) {
                                generatedId = keys.getLong(1);
                            }
                        }
                    }
                    case "update", "delete", "exec" -> rowsAffected = ps.executeUpdate();
                    default -> throw new IllegalArgumentException("Ação inválida: '" + action + "'. "
                            + "Use query, insert, update, delete ou exec.");
                }
            }
        } catch (SQLException ex) {
            throw fail(ex);
        } finally {
            // 5. Fecha a conexão que ele mesmo abriu, dando certo ou errado
            if (ownConnection) {
                disconnect();
            }
        }
    }

    private static void bind(PreparedStatement ps, NamedSql named, Map<String, Object> args)
            throws SQLException {
        for (int i = 0; i < named.names().size(); i++) {
            String name = named.names().get(i);
            if (args == null || !args.containsKey(name)) {
                throw new IllegalArgumentException("O parâmetro @" + name + " não tem valor em args.");
            }
            ps.setObject(i + 1, args.get(name));   // JDBC conta a partir de 1
        }
    }

    private static List<Map<String, Object>> readAll(ResultSet rs) throws SQLException {
        ResultSetMetaData meta = rs.getMetaData();
        List<Map<String, Object>> rows = new ArrayList<>();

        while (rs.next()) {
            Map<String, Object> row = new LinkedHashMap<>();
            for (int i = 1; i <= meta.getColumnCount(); i++) {
                row.put(meta.getColumnLabel(i), rs.getObject(i));
            }
            rows.add(row);
        }
        return rows;
    }

    private OrmException fail(SQLException ex) {
        dbError = ex.getMessage();
        return new OrmException(OrmErrorKind.OTHER, dbError, "", ex);
    }

    @Override
    public String getStatus() {
        return dbError.isEmpty() ? "success" : "fail";
    }

    @Override
    public String getDbError() {
        return dbError;
    }

    @Override
    public long getRowsAffected() {
        return rowsAffected;
    }

    @Override
    public long getGeneratedId() {
        return generatedId;
    }

    @Override
    public List<Map<String, Object>> getDataTable() {
        return dataTable;
    }

    @Override
    public String getLastSqlSentence() {
        return lastSql;
    }

    // Preenchidos no capítulo 8
    @Override
    public List<Map<String, Object>> select(String table, String fields, String where,
                                            Map<String, Object> args, String order) {
        throw new UnsupportedOperationException();
    }

    @Override
    public void insert(String table, Map<String, Object> data) {
        throw new UnsupportedOperationException();
    }

    @Override
    public void update(String table, Map<String, Object> data, String where, Map<String, Object> args) {
        throw new UnsupportedOperationException();
    }

    @Override
    public void delete(String table, String where, Map<String, Object> args) {
        throw new UnsupportedOperationException();
    }

    // Preenchidos no capítulo 9
    @Override
    public void beginTransaction() {
        throw new UnsupportedOperationException();
    }

    @Override
    public void commit() {
        throw new UnsupportedOperationException();
    }

    @Override
    public void rollback() {
        throw new UnsupportedOperationException();
    }

    @Override
    public void close() {
        disconnect();
    }
}
