package meuorm.drivers;

import java.util.ArrayList;
import java.util.List;

// Troca cada @nome do SQL por "?" e guarda os nomes na ordem em que aparecem
public record NamedSql(String sql, List<String> names) {

    public static NamedSql parse(String text) {
        StringBuilder out = new StringBuilder();
        List<String> names = new ArrayList<>();
        boolean insideString = false;

        for (int i = 0; i < text.length(); i++) {
            char c = text.charAt(i);

            if (c == '\'') {
                insideString = !insideString;   // @ dentro de 'texto' não é parâmetro
            }

            boolean startsName = c == '@' && i + 1 < text.length()
                    && (Character.isLetter(text.charAt(i + 1)) || text.charAt(i + 1) == '_');

            if (!insideString && startsName) {
                int end = i + 1;
                while (end < text.length()
                        && (Character.isLetterOrDigit(text.charAt(end)) || text.charAt(end) == '_')) {
                    end++;
                }
                names.add(text.substring(i + 1, end));
                out.append('?');
                i = end - 1;
            } else {
                out.append(c);
            }
        }

        return new NamedSql(out.toString(), names);
    }
}
