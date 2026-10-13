using MeuOrm.Core;

DotEnv.Load();

using var orm = new Orm();

orm.Exec("SELECT id, nome, email FROM cliente", "query");

foreach (System.Data.DataRow row in orm.GetDataTable().Rows)
    Console.WriteLine($"{row["id"]} - {row["nome"]}");

Console.WriteLine($"{orm.GetRowsAffected()} linha(s). SQL: {orm.GetLastSqlSentence()}");
