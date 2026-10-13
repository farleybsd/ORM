package meuorm;

import java.util.Map;
import meuorm.core.DotEnv;
import meuorm.core.Orm;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();

        try (Orm orm = new Orm()) {
            orm.exec("SELECT id, nome, email FROM cliente", "query");

            for (Map<String, Object> row : orm.getDataTable()) {
                System.out.println(row.get("id") + " - " + row.get("nome"));
            }

            System.out.println(orm.getRowsAffected() + " linha(s). SQL: " + orm.getLastSqlSentence());
        }
    }
}
