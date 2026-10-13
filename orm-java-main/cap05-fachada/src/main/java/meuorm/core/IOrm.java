package meuorm.core;

import java.util.List;
import java.util.Map;

public interface IOrm extends AutoCloseable {

    // A porta única: todo SQL passa por aqui
    void exec(String sql, String action, Map<String, Object> args);

    default void exec(String sql, String action) {
        exec(sql, action, null);
    }

    // Atalhos de CRUD (montam o SQL e chamam o exec)
    List<Map<String, Object>> select(String table, String fields, String where,
                                     Map<String, Object> args, String order);

    default List<Map<String, Object>> select(String table, String fields, String where,
                                             Map<String, Object> args) {
        return select(table, fields, where, args, "");
    }

    void insert(String table, Map<String, Object> data);

    void update(String table, Map<String, Object> data, String where, Map<String, Object> args);

    void delete(String table, String where, Map<String, Object> args);

    // Transação manual
    void beginTransaction();

    void commit();

    void rollback();

    // Estado da última operação
    String getStatus();

    String getDbError();

    long getRowsAffected();

    long getGeneratedId();

    List<Map<String, Object>> getDataTable();

    String getLastSqlSentence();

    // Libera a conexão e desfaz transação esquecida (sem "throws Exception")
    @Override
    void close();
}
