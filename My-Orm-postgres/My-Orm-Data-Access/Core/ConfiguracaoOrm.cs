using Microsoft.Extensions.Configuration;

namespace My_Orm_Data_Access.Core;

public static class ConfiguracaoOrm
{
    // Guarda a configuração recebida da API para usar depois
    private static IConfiguration? _config;

    // Recebe o IConfiguration já montado pelo ASP.NET (appsettings, env vars, etc.)
    public static void Load(IConfiguration config)
    {
        _config = config;
    }

    // Busca a connection string obrigatória e explica o erro se ela não existir
    public static string GetConnectionString(string name = "DefaultConnection")
    {
        if (_config is null)
            throw new InvalidOperationException(
                "ConfiguracaoOrm não foi carregada. Chame ConfiguracaoOrm.Load() no Program.cs");

        return _config.GetConnectionString(name)
            ?? throw new InvalidOperationException(
                $"A connection string '{name}' não foi encontrada. Confira o appsettings.json");
    }
}
