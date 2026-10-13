using System.Data;
using MeuOrm.Core;
using MeuOrm.Models;

DotEnv.Load();
Console.WriteLine($"Driver: {DotEnv.Get("ORM_DRIVER")}\n");

// 1. Criar
var maria = new Cliente { Nome = "Maria", Email = $"maria{DateTime.Now.Ticks}@email.com" };
maria.Save();
Console.WriteLine($"1. Criada: id {maria.Id}");

// 2. Duplicado: o banco recusa e o ORM traduz
try
{
    new Cliente { Nome = "Outra Maria", Email = maria.Email }.Save();
}
catch (OrmException ex) when (ex.Kind == OrmErrorKind.Duplicate)
{
    Console.WriteLine("2. E-mail repetido recusado, como esperado");
}

// 3. Ler e alterar só um campo (dirty-check)
var c = new Cliente();
c.Load(maria.Id);
c.Nome = "Maria Souza";
c.Save();
Console.WriteLine($"3. Alterada: {c.Nome}");

// 4. Save sem mudança não vai ao banco
Console.WriteLine($"4. Gravou de novo? {c.Save()}");

// 5. Conflito: dois usuários editando ao mesmo tempo
var ana = new Cliente();   ana.Load(maria.Id);
var bruno = new Cliente(); bruno.Load(maria.Id);

ana.Ativo = false;
ana.Save();

bruno.Nome = "Nome do Bruno";
try
{
    bruno.Save();
}
catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
{
    Console.WriteLine("5. Conflito detectado: Bruno precisa recarregar");
}

// 6. Listar com SQL visível
using (var orm = new Orm())
{
    DataTable dt = orm.Select("cliente", "id, nome, ativo", "", null, "id DESC");
    Console.WriteLine($"6. {dt.Rows.Count} cliente(s). SQL: {orm.GetLastSqlSentence()}");

    foreach (DataRow row in dt.Rows)
        Console.WriteLine($"   {row["id"],4} | {row["nome"],-20} | ativo: {row["ativo"]}");
}

// 7. Apagar
c.Load(maria.Id);
c.Delete();
Console.WriteLine($"7. Apagada. Ainda existe? {new Cliente().Load(maria.Id)}");
