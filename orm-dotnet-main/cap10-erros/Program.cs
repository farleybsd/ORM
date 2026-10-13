using MeuOrm.Core;

DotEnv.Load();
using var orm = new Orm();

string email = $"ana-{Guid.NewGuid():N}@email.com";
orm.Insert("cliente", new() { ["nome"] = "Ana", ["email"] = email });

try
{
    orm.Insert("cliente", new() { ["nome"] = "Ana de novo", ["email"] = email });
}
catch (OrmException ex) when (ex.Kind == OrmErrorKind.Duplicate)
{
    Console.WriteLine("Este e-mail já está cadastrado.");
}
catch (OrmException ex) when (ex.Kind == OrmErrorKind.Connection)
{
    Console.WriteLine("Banco fora do ar. Tente em alguns minutos.");
}

// Os outros tipos de erro, um de cada
Testar("Chave estrangeira", () => orm.Insert("pedido", new() { ["cliente_id"] = -1L, ["total"] = 1m }));
Testar("Campo obrigatório", () => orm.Insert("cliente", new() { ["nome"] = null, ["email"] = "x@x.com" }));
Testar("Tabela inexistente", () => orm.Exec("SELECT * FROM nao_existe", "query"));
Testar("Coluna inexistente", () => orm.Exec("SELECT coluna_x FROM cliente", "query"));

static void Testar(string titulo, Action acao)
{
    try { acao(); }
    catch (OrmException ex) { Console.WriteLine($"{titulo}: Kind={ex.Kind} | {ex.Message}"); }
}
