package meuorm.models;

public final class Naming {

    private Naming() {
    }

    // "PedidoItem" -> "pedido_item", "insertedAt" -> "inserted_at"
    public static String toSnake(String name) {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < name.length(); i++) {
            char c = name.charAt(i);

            if (Character.isUpperCase(c)) {
                if (i > 0) {
                    sb.append('_');   // maiúscula no meio = começa palavra nova
                }
                sb.append(Character.toLowerCase(c));
            } else {
                sb.append(c);
            }
        }

        return sb.toString();
    }
}
