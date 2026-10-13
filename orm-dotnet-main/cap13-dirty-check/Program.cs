using MeuOrm.Core;
using MeuOrm.Models;

DotEnv.Load();

var nova = new Cliente { Nome = "Maria", Email = $"maria-{Guid.NewGuid():N}@email.com" };
nova.Save();

var c = new Cliente();
c.Load(nova.Id);

c.Nome = "Maria Souza";
Console.WriteLine($"Gravou? {c.Save()}");   // True: só nome e updated_at vão no UPDATE

bool gravou = c.Save();
Console.WriteLine($"Gravou de novo? {gravou}");   // False: nada mudou desde o último Save
