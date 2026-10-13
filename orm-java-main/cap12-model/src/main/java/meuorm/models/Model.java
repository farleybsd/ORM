package meuorm.models;

import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import meuorm.core.Orm;
import meuorm.core.Params;

public abstract class Model {

    // Colunas que toda tabela do sistema tem
    protected long id;
    protected long insertedAt;
    protected long updatedAt;

    public long getId() {
        return id;
    }

    public long getInsertedAt() {
        return insertedAt;
    }

    public long getUpdatedAt() {
        return updatedAt;
    }

    // Nome da tabela pela convenção: Cliente -> cliente, PedidoItemModel -> pedido_item
    protected String tableName() {
        String name = getClass().getSimpleName();
        if (name.endsWith("Model")) {
            name = name.substring(0, name.length() - 5);   // tira as 5 letras de "Model"
        }
        return Naming.toSnake(name);
    }

    // Os campos que viram colunas: os da classe e das mães, menos static e transient
    protected List<Field> columns() {
        List<Field> result = new ArrayList<>();

        for (Class<?> type = getClass(); type != Object.class; type = type.getSuperclass()) {
            for (Field field : type.getDeclaredFields()) {
                int mod = field.getModifiers();
                if (Modifier.isStatic(mod) || Modifier.isTransient(mod)) {
                    continue;
                }
                field.setAccessible(true);   // permite ler e escrever campos private
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
            return false;   // não achou: o objeto continua como estava
        }

        fill(rows.get(0));
        return true;
    }

    public void save() {
        // Monta o mapa coluna -> valor a partir dos campos
        Map<String, Object> data = new LinkedHashMap<>();

        for (Field field : columns()) {
            String column = Naming.toSnake(field.getName());

            if (column.equals("id") || column.equals("inserted_at") || column.equals("updated_at")) {
                continue;   // controlados pelo próprio Model
            }

            data.put(column, read(field));
        }

        long now = System.currentTimeMillis();

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
                orm.update(tableName(), data, "id = @id", Params.of("id", id));
                updatedAt = now;
            }
        }
    }

    public void delete() {
        if (id == 0) {
            throw new IllegalStateException("Não dá para apagar um registro que nunca foi gravado.");
        }

        try (Orm orm = new Orm()) {
            orm.delete(tableName(), "id = @id", Params.of("id", id));
        }

        id = 0;
    }

    // Copia uma linha do banco para os campos do objeto
    protected void fill(Map<String, Object> row) {
        for (Field field : columns()) {
            String column = Naming.toSnake(field.getName());

            if (!row.containsKey(column)) {
                continue;   // campo sem coluna correspondente
            }

            write(field, convert(row.get(column), field.getType()));
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
