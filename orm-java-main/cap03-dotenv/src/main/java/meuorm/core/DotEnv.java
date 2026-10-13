package meuorm.core;

import java.io.IOException;
import java.io.UncheckedIOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;

public final class DotEnv {

    // O Java não deixa alterar as variáveis de ambiente do processo: guardamos aqui
    private static final Map<String, String> values = new HashMap<>();

    private DotEnv() {
    }

    public static void load() {
        load(".env");
    }

    // Lê o arquivo e guarda cada CHAVE=valor
    public static void load(String path) {
        Path file = Path.of(path);
        if (!Files.exists(file)) {
            return; // em produção as variáveis podem vir do próprio servidor
        }

        try {
            for (String line : Files.readAllLines(file, StandardCharsets.UTF_8)) {
                String text = line.trim();

                if (text.isEmpty() || text.startsWith("#")) {
                    continue; // linha vazia ou comentário
                }

                int pos = text.indexOf('=');
                if (pos <= 0) {
                    continue; // linha sem "=" é ignorada
                }

                values.put(text.substring(0, pos).trim(), text.substring(pos + 1).trim());
            }
        } catch (IOException ex) {
            throw new UncheckedIOException("Não foi possível ler o arquivo " + path, ex);
        }
    }

    // Busca uma variável obrigatória: primeiro no .env, depois no ambiente do servidor
    public static String get(String key) {
        String value = values.containsKey(key) ? values.get(key) : System.getenv(key);
        if (value == null) {
            throw new IllegalStateException(
                    "A variável '" + key + "' não foi encontrada. Confira o arquivo .env");
        }
        return value;
    }
}
