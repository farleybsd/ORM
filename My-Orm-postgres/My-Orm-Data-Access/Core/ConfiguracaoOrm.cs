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
        return Config().GetConnectionString(name)
            ?? throw new InvalidOperationException(
                $"A connection string '{name}' não foi encontrada. Confira o appsettings.json");
    }

    // Busca o nome da classe do driver (ex.: "Postgres") na seção "Orm" do appsettings
    public static string GetDriver()
    {
        return Config()["Orm:Driver"]
            ?? throw new InvalidOperationException(
                "O driver do ORM não foi encontrado. Confira 'Orm:Driver' no appsettings.json");
    }

    // Garante que o Load foi chamado antes de qualquer leitura
    private static IConfiguration Config()
    {
        return _config
            ?? throw new InvalidOperationException(
                "ConfiguracaoOrm não foi carregada. Chame ConfiguracaoOrm.Load() no Program.cs");
    }
}
