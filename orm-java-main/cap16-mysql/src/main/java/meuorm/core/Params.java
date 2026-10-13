package meuorm.core;

import java.util.LinkedHashMap;
import java.util.Map;

public final class Params {

    private Params() {
    }

    // Params.of("nome", "Maria", "ativo", true): aceita null e mantém a ordem
    public static Map<String, Object> of(Object... pairs) {
        if (pairs.length % 2 != 0) {
            throw new IllegalArgumentException("Params.of precisa de pares: nome, valor, nome, valor...");
        }

        Map<String, Object> map = new LinkedHashMap<>();
        for (int i = 0; i < pairs.length; i += 2) {
            map.put((String) pairs[i], pairs[i + 1]);
        }
        return map;
    }
}
