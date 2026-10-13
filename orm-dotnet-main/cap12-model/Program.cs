using MeuOrm.Core;
using MeuOrm.Models;

DotEnv.Load();

// No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
var novo = new Cliente { Nome = "Carla", Email = $"carla-{Guid.NewGuid():N}@email.com" };
novo.Save();                                       // INSERT
Console.WriteLine($"Criada com id {novo.Id}");

var carla = new Cliente();
if (carla.Load(novo.Id))
{
    carla.Nome = "Carla Souza";
    carla.Save();                                  // UPDATE (ainda grava todas as colunas)
    Console.WriteLine($"Alterada: {carla.Nome}");
}
