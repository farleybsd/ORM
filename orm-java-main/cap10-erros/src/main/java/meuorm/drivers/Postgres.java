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
import java.util.stream.Collectors;

import org.postgresql.util.PSQLException;
import org.postgresql.util.ServerErrorMessage;

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
        OrmException error = translateError(ex);   // Capítulo 10: traduz o erro do banco
        dbError = error.getMessage();
        return error;
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

    // ===== Capítulo 8: CRUD =====
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
        // data = {nome=Maria, email=maria@email.com}
        String fields = String.join(", ", data.keySet());                         // nome, email
        String values = data.keySet().stream()
                .map(key -> "@" + key).collect(Collectors.joining(", "));         // @nome, @email

        exec("INSERT INTO " + table + " (" + fields + ") VALUES (" + values + ")", "insert", data);
    }

    @Override
    public void update(String table, Map<String, Object> data, String where, Map<String, Object> args) {
        if (where == null || where.isBlank()) {
            throw new IllegalArgumentException("Update sem WHERE bloqueado: isso alteraria a tabela inteira.");
        }

        // Junta os parâmetros do WHERE com os valores novos
        Map<String, Object> parameters = new LinkedHashMap<>();
        if (args != null) {
            parameters.putAll(args);
        }

        List<String> sets = new ArrayList<>();
        for (Map.Entry<String, Object> entry : data.entrySet()) {
            sets.add(entry.getKey() + " = @v_" + entry.getKey());   // nome = @v_nome
            parameters.put("v_" + entry.getKey(), entry.getValue());
        }

        exec("UPDATE " + table + " SET " + String.join(", ", sets) + " WHERE " + where, "update", parameters);
    }

    @Override
    public void delete(String table, String where, Map<String, Object> args) {
        if (where == null || where.isBlank()) {
            throw new IllegalArgumentException("Delete sem WHERE bloqueado: isso apagaria a tabela inteira.");
        }

        exec("DELETE FROM " + table + " WHERE " + where, "delete", args);
    }

    // ===== Capítulo 9: transações =====
    @Override
    public void beginTransaction() {
        if (inTransaction) {
            throw new IllegalStateException("Já existe uma transação aberta.");
        }

        try {
            connect();
            connection.setAutoCommit(false);   // a partir daqui, nada é gravado sem commit
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
            return; // nada a desfazer
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

    @Override
    public void close() {
        // Esqueceram o commit? Desfaz tudo. Nunca grave pela metade.
        try {
            rollback();
        } catch (RuntimeException ignored) {
            // a conexão pode já ter caído; o banco desfaz sozinho
        }
        inTransaction = false;
        disconnect();
    }

    // ===== Capítulo 10: tradução de erros =====
    private static OrmException translateError(SQLException ex) {
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
