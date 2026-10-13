using MeuOrm.Core;
using MeuOrm.Models;

DotEnv.Load();

var registro = new Cliente { Nome = "Cliente", Email = $"conflito-{Guid.NewGuid():N}@email.com" };
registro.Save();

var ana = new Cliente();
ana.Load(registro.Id);

var bruno = new Cliente();
bruno.Load(registro.Id);        // os dois leram a mesma versão

ana.Nome = "Nome da Ana";
ana.Save();                     // grava e muda o updated_at
Console.WriteLine("Ana gravou.");

bruno.Nome = "Nome do Bruno";
try
{
    bruno.Save();               // condição falha: zero linhas
}
catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
{
    Console.WriteLine($"Bruno: {ex.Message}");
}
