using MeuOrm.Core;

DotEnv.Load();
using var orm = new Orm();

// No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
orm.Insert("cliente", new() { ["nome"] = "João", ["email"] = $"joao-{Guid.NewGuid():N}@email.com" });
long id = orm.GetGeneratedId();
Console.WriteLine($"Novo id: {id}");

orm.Update("cliente", new() { ["nome"] = "João Pedro" }, "id = @id", new() { ["id"] = id });
Console.WriteLine($"{orm.GetRowsAffected()} linha(s) alterada(s)");

var dt = orm.Select("cliente", "nome", "id = @id", new() { ["id"] = id });
Console.WriteLine(dt.Rows[0]["nome"]);   // João Pedro

orm.Delete("cliente", "id = @id", new() { ["id"] = id });
Console.WriteLine(orm.GetLastSqlSentence());  // DELETE FROM cliente WHERE id = @id

// A trava do WHERE
try
{
    orm.Update("cliente", new() { ["nome"] = "Todo mundo" }, "");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}
