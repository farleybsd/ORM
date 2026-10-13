package meuorm;

import java.lang.reflect.Field;

import meuorm.models.Cliente;
import meuorm.models.Naming;

public class Main {

    public static void main(String[] args) throws IllegalAccessException, NoSuchFieldException {
        Cliente cliente = new Cliente();
        cliente.setId(7);
        cliente.setNome("Maria");
        cliente.setEmail("maria@email.com");

        // 1. Descobrir os campos
        Class<?> type = cliente.getClass();            // a "ficha técnica" da classe
        Field[] fields = type.getDeclaredFields();     // todos os campos declarados nela

        for (Field field : fields) {
            field.setAccessible(true);                 // libera o acesso a campos private

            // 2. Ler o valor pelo nome
            Object value = field.get(cliente);
            System.out.println(field.getName() + " (" + field.getType().getSimpleName() + ") = " + value);
        }

        // 3. Alterar um valor pelo nome
        Field nome = type.getDeclaredField("nome");
        nome.setAccessible(true);
        nome.set(cliente, "Maria Silva");
        System.out.println(cliente.getNome());          // Maria Silva

        for (String s : new String[] {"Cliente", "PedidoItem", "insertedAt", "id"}) {
            System.out.println(s + " -> " + Naming.toSnake(s));
        }
    }
}
