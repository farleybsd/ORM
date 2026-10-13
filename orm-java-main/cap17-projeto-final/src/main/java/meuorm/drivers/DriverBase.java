package meuorm.drivers;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.ResultSetMetaData;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import meuorm.core.IOrm;
import meuorm.core.OrmException;

public abstract class DriverBase implements IOrm {

    protected Connection connection;
    protected boolean inTransaction;

    protected String dbError = "";
    protected String lastSql = "";
    protected long rowsAffected;
    protected long generatedId;
    protected List<Map<String, Object>> dataTable = new ArrayList<>();

    // ===== Os buracos que cada banco preenche =====
    protected abstract Connection createConnection() throws SQLException;

    protected abstract OrmException translateError(SQLException ex);

    // ===== O roteiro comum =====
    @Override
    public void exec(String sql, String action, Map<String, Object> args) {
        lastSql = sql;
        dbError = "";
        rowsAffected = 0;
        generatedId = 0;
        dataTable = new ArrayList<>();

        boolean ownConnection = !inTransaction;

        try {
            if (ownConnection) {
                connect();
            }

            NamedSql named = NamedSql.parse(sql);
            boolean isInsert = action.equals("insert");

            try (PreparedStatement ps = isInsert
                    ? connection.prepareStatement(named.sql(), new String[] {"id"})
                    : connection.prepareStatement(named.sql())) {

                bind(ps, named, args);

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
                    default -> throw new IllegalArgumentException("Ação inválida: '" + action + "'.");
                }
            }
        } catch (SQLException ex) {
            throw fail(ex);
        } finally {
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
        OrmException error = translateError(ex);   // cada banco traduz seus códigos
        dbError = error.getMessage();
        return error;
    }

    @Override
    public List<Map<String, Object>> select(String table, String fields, String where,
                                            Map<String, Object> args, String order) {
        String sql = "SELECT " + fields + " FROM " + table;
        if (!where.isBlank()) {
            sql += " WHERE " + where;
        }
        if (!order.isBlank()) {
            sql += " ORDER BY " + order;
        }

        exec(sql, "query", args);
        return dataTable;
    }

    @Override
    public void insert(String table, Map<String, Object> data) {
        String fields = String.join(", ", data.keySet());
        String values = data.keySet().stream().map(k -> "@" + k).collect(Collectors.joining(", "));
        exec("INSERT INTO " + table + " (" + fields + ") VALUES (" + values + ")", "insert", data);
    }

    @Override
    public void update(String table, Map<String, Object> data, String where, Map<String, Object> args) {
        if (where == null || where.isBlank()) {
            throw new IllegalArgumentException("Update sem WHERE bloqueado.");
        }

        Map<String, Object> parameters = new LinkedHashMap<>();
        if (args != null) {
            parameters.putAll(args);
        }

        List<String> sets = new ArrayList<>();
        for (Map.Entry<String, Object> entry : data.entrySet()) {
            sets.add(entry.getKey() + " = @v_" + entry.getKey());
            parameters.put("v_" + entry.getKey(), entry.getValue());
        }

        exec("UPDATE " + table + " SET " + String.join(", ", sets) + " WHERE " + where, "update", parameters);
    }

    @Override
    public void delete(String table, String where, Map<String, Object> args) {
        if (where == null || where.isBlank()) {
            throw new IllegalArgumentException("Delete sem WHERE bloqueado.");
        }

        exec("DELETE FROM " + table + " WHERE " + where, "delete", args);
    }

    @Override
    public void beginTransaction() {
        if (inTransaction) {
            throw new IllegalStateException("Já existe uma transação aberta.");
        }

        try {
            connect();
            connection.setAutoCommit(false);
        } catch (SQLException ex) {
            disconnect();
            throw fail(ex);
        }
        inTransaction = true;
    }

    @Override
    public void commit() {
        if (!inTransaction) {
            throw new IllegalStateException("Nenhuma transação aberta para confirmar.");
        }

        try {
            connection.commit();
        } catch (SQLException ex) {
            throw fail(ex);
        } finally {
            endTransaction();
        }
    }

    @Override
    public void rollback() {
        if (!inTransaction) {
            return;
        }

        try {
            connection.rollback();
        } catch (SQLException ex) {
            throw fail(ex);
        } finally {
            endTransaction();
        }
    }

    private void endTransaction() {
        inTransaction = false;
        disconnect();
    }

    private void connect() throws SQLException {
        connection = createConnection();
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

    @Override
    public void close() {
        try {
            rollback();
        } catch (RuntimeException ignored) {
            // conexão já caída: o banco desfaz sozinho
        }
        inTransaction = false;
        disconnect();
    }
}
