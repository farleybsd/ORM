package meuorm.core;

import java.util.List;
import java.util.Map;

public class Orm implements IOrm {

    private final IOrm driver;

    public Orm() {
        String name = DotEnv.get("ORM_DRIVER");            // ex.: "Postgres"
        String className = "meuorm.drivers." + name;       // ex.: meuorm.drivers.Postgres

        Class<?> type;
        try {
            type = Class.forName(className);
        } catch (ClassNotFoundException ex) {
            throw new IllegalStateException(
                    "Driver '" + name + "' não encontrado. Confira ORM_DRIVER no .env "
                    + "(maiúsculas e minúsculas fazem diferença: Postgres, não postgres).");
        }

        if (!IOrm.class.isAssignableFrom(type)) {
            throw new IllegalStateException("A classe '" + name + "' existe, mas não implementa IOrm.");
        }

        try {
            driver = (IOrm) type.getDeclaredConstructor().newInstance();
        } catch (ReflectiveOperationException ex) {
            throw new IllegalStateException("Não foi possível criar o driver '" + name + "'.", ex);
        }
    }

    // Daqui para baixo: cada método só repassa para o driver
    @Override
    public void exec(String sql, String action, Map<String, Object> args) {
        driver.exec(sql, action, args);
    }

    @Override
    public List<Map<String, Object>> select(String table, String fields, String where,
                                            Map<String, Object> args, String order) {
        return driver.select(table, fields, where, args, order);
    }

    @Override
    public void insert(String table, Map<String, Object> data) {
        driver.insert(table, data);
    }

    @Override
    public void update(String table, Map<String, Object> data, String where, Map<String, Object> args) {
        driver.update(table, data, where, args);
    }

    @Override
    public void delete(String table, String where, Map<String, Object> args) {
        driver.delete(table, where, args);
    }

    @Override
    public void beginTransaction() {
        driver.beginTransaction();
    }

    @Override
    public void commit() {
        driver.commit();
    }

    @Override
    public void rollback() {
        driver.rollback();
    }

    @Override
    public String getStatus() {
        return driver.getStatus();
    }

    @Override
    public String getDbError() {
        return driver.getDbError();
    }

    @Override
    public long getRowsAffected() {
        return driver.getRowsAffected();
    }

    @Override
    public long getGeneratedId() {
        return driver.getGeneratedId();
    }

    @Override
    public List<Map<String, Object>> getDataTable() {
        return driver.getDataTable();
    }

    @Override
    public String getLastSqlSentence() {
        return driver.getLastSqlSentence();
    }

    @Override
    public void close() {
        driver.close();
    }
}
