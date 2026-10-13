using MeuOrm.Core;

DotEnv.Load();
using var orm = new Orm();

// O que o usuário digitou no campo de e-mail:
string email = "' OR '1'='1";

// ERRADO: o valor colado no SQL vira comando
orm.Exec("SELECT * FROM cliente WHERE email = '" + email + "'", "query");
Console.WriteLine($"Concatenado: {orm.GetRowsAffected()} linha(s) (devolveu TODOS os clientes)");

// CERTO: o valor viaja como parâmetro
orm.Exec("SELECT * FROM cliente WHERE email = @email", "query",
         new() { ["email"] = email });
Console.WriteLine($"Com parâmetro: {orm.GetRowsAffected()} linha(s)");

// LIKE: o % vai dentro do valor
string texto = "ari";
orm.Exec("SELECT * FROM cliente WHERE nome ILIKE @busca", "query",
         new() { ["busca"] = "%" + texto + "%" });
Console.WriteLine($"LIKE '%{texto}%': {orm.GetRowsAffected()} linha(s)");

// Ordenação escolhida pelo usuário: compare com uma lista fixa
string escolhaDoUsuario = "email; DROP TABLE cliente";
string[] permitidas = { "nome", "email", "inserted_at" };
string order = permitidas.Contains(escolhaDoUsuario) ? escolhaDoUsuario : "nome";
Console.WriteLine($"Ordenação usada: {order}");
