package meuorm.models;

import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

import meuorm.core.Orm;
import meuorm.core.OrmErrorKind;
import meuorm.core.OrmException;
import meuorm.core.Params;

public abstract class Model {

    protected long id;
    protected long insertedAt;
    protected long updatedAt;

    // transient: não é coluna, o ORM ignora
    private final transient Map<String, Object> snapshot = new HashMap<>();

    public long getId() {
        return id;
    }

    public long getInsertedAt() {
        return insertedAt;
    }

    public long getUpdatedAt() {
        return updatedAt;
    }

    protected String tableName() {
        Table table = getClass().getAnnotation(Table.class);
        if (table != null) {
            return table.value();
        }

        String name = getClass().getSimpleName();
        if (name.endsWith("Model")) {
            name = name.substring(0, name.length() - 5);
        }
        return Naming.toSnake(name);
    }

    protected List<Field> columns() {
        List<Field> result = new ArrayList<>();

        for (Class<?> type = getClass(); type != Object.class; type = type.getSuperclass()) {
            for (Field field : type.getDeclaredFields()) {
                int mod = field.getModifiers();
                if (Modifier.isStatic(mod) || Modifier.isTransient(mod)) {
                    continue;
                }
                field.setAccessible(true);
                result.add(field);
            }
        }
        return result;
    }

    public boolean load(long id) {
        List<Map<String, Object>> rows;
        try (Orm orm = new Orm()) {
            rows = orm.select(tableName(), "*", "id = @id", Params.of("id", id));
        }

        if (rows.isEmpty()) {
            return false;
        }

        fill(rows.get(0));
        takeSnapshot();
        return true;
    }

    public boolean loadQuery(String sql, Map<String, Object> args) {
        List<Map<String, Object>> rows;
        try (Orm orm = new Orm()) {
            orm.exec(sql, "query", args);
            rows = orm.getDataTable();
        }

        if (rows.isEmpty()) {
            return false;
        }

        fill(rows.get(0));
        takeSnapshot();
        return true;
    }

    public boolean save() {
        Map<String, Object> data = new LinkedHashMap<>();

        for (Field field : columns()) {
            String column = Naming.toSnake(field.getName());

            if (column.equals("id") || column.equals("inserted_at") || column.equals("updated_at")) {
                continue;
            }

            if (field.isAnnotationPresent(CalcField.class)) {
                continue;
            }

            Object value = read(field);

            if (id != 0 && snapshot.containsKey(column) && Objects.equals(snapshot.get(column), value)) {
                continue;
            }

            data.put(column, value);
        }

        if (id != 0 && data.isEmpty()) {
            return false;
        }

        long now = Math.max(System.currentTimeMillis(), updatedAt + 1);

        try (Orm orm = new Orm()) {
            if (id == 0) {
                data.put("inserted_at", now);
                data.put("updated_at", now);
                orm.insert(tableName(), data);

                id = orm.getGeneratedId();
                insertedAt = now;
                updatedAt = now;
            } else {
                data.put("updated_at", now);

                orm.update(tableName(), data,
                        "id = @id AND updated_at = @old_updated_at",
                        Params.of("id", id, "old_updated_at", updatedAt));

                if (orm.getRowsAffected() == 0) {
                    throw new OrmException(OrmErrorKind.CONCURRENCY,
                            "Este registro foi alterado ou excluído por outro usuário. "
                            + "Recarregue os dados e tente novamente.");
                }

                updatedAt = now;
            }
        }

        takeSnapshot();
        return true;
    }

    public void delete() {
        if (id == 0) {
            throw new IllegalStateException("Não dá para apagar um registro que nunca foi gravado.");
        }

        try (Orm orm = new Orm()) {
            orm.delete(tableName(), "id = @id", Params.of("id", id));
        }

        id = 0;
        snapshot.clear();
    }

    protected void fill(Map<String, Object> row) {
        for (Field field : columns()) {
            String column = Naming.toSnake(field.getName());

            if (!row.containsKey(column)) {
                continue;
            }

            write(field, convert(row.get(column), field.getType()));
        }
    }

    private void takeSnapshot() {
        snapshot.clear();
        for (Field field : columns()) {
            snapshot.put(Naming.toSnake(field.getName()), read(field));
        }
    }

    // Converte o que veio do banco para o tipo declarado do campo
    private static Object convert(Object value, Class<?> type) {
        if (value == null) {
            if (!type.isPrimitive()) {
                return null;
            }
            return type == boolean.class ? Boolean.FALSE : (Object) 0;
        }
        if ((type == long.class || type == Long.class) && value instanceof Number n) {
            return n.longValue();
        }
        if ((type == int.class || type == Integer.class) && value instanceof Number n) {
            return n.intValue();
        }
        if ((type == double.class || type == Double.class) && value instanceof Number n) {
            return n.doubleValue();
        }
        if ((type == boolean.class || type == Boolean.class) && value instanceof Number n) {
            return n.intValue() != 0;
        }
        if (type == BigDecimal.class && value instanceof Number n && !(value instanceof BigDecimal)) {
            return new BigDecimal(n.toString());
        }
        if (type == String.class && !(value instanceof String)) {
            return value.toString();
        }
        return value;
    }

    private Object read(Field field) {
        try {
            return field.get(this);
        } catch (IllegalAccessException ex) {
            throw new IllegalStateException(ex);
        }
    }

    private void write(Field field, Object value) {
        try {
            field.set(this, value);
        } catch (IllegalAccessException ex) {
            throw new IllegalStateException(ex);
        }
    }
}
