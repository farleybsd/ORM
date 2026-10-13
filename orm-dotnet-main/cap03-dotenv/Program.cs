using MeuOrm.Core;

DotEnv.Load();
Console.WriteLine($"Driver: {DotEnv.Get("ORM_DRIVER")}");
Console.WriteLine($"Banco: {DotEnv.Get("DB_NAME")}");
