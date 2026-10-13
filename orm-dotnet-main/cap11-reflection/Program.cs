using System.Reflection;
using MeuOrm.Models;

var cliente = new Cliente { Id = 7, Nome = "Maria", Email = "maria@email.com" };

// 1. Descobrir as propriedades
Type type = cliente.GetType();                   // a "ficha técnica" da classe
PropertyInfo[] props = type.GetProperties();     // todas as propriedades públicas

foreach (PropertyInfo prop in props)
{
    // 2. Ler o valor pelo nome
    object? value = prop.GetValue(cliente);
    Console.WriteLine($"{prop.Name} ({prop.PropertyType.Name}) = {value}");
}

// 3. Alterar um valor pelo nome
PropertyInfo nome = type.GetProperty("Nome")!;
nome.SetValue(cliente, "Maria Silva");
Console.WriteLine(cliente.Nome);                  // Maria Silva

foreach (var s in new[]{"Cliente","PedidoItem","Nome","InsertedAt","Id"}) Console.WriteLine($"{s} -> {Naming.ToSnake(s)}");
