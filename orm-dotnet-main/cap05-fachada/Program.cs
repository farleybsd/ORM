using MeuOrm.Core;

DotEnv.Load();

// Ainda não existe nenhum driver. Rodando agora, a fachada procura
// a classe MeuOrm.Drivers.Postgres e explica o que está faltando.
try
{
    using var orm = new Orm();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}
