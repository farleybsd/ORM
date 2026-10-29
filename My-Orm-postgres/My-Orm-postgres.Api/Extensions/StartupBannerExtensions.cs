using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace My_Orm_postgres.Api.Extensions;

public static class StartupBannerExtensions
{
    private const string Descricao =
        "Cadastro de clientes da Loja (criar, consultar, listar, atualizar e excluir) " +
        "sobre PostgreSQL, usando o ORM próprio My-Orm-Data-Access.";

    /// <summary>
    /// Registra no log, assim que a aplicação termina de subir, uma mensagem informando
    /// que a API está no ar, o que ela faz e em quais endereços está escutando.
    /// </summary>
    /// <remarks>
    /// As mensagens são logs estruturados, então também aparecem no painel do Aspire.
    /// </remarks>
    public static WebApplication UseStartupBanner(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Loja.Api");

            var enderecos = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses ?? [];

            logger.LogInformation("🚀 {Aplicacao} no ar! Ambiente: {Ambiente}",
                app.Environment.ApplicationName, app.Environment.EnvironmentName);
            logger.LogInformation("🛒 {Descricao}", Descricao);

            foreach (var endereco in enderecos)
                logger.LogInformation("🌐 Escutando em {Endereco} · 📖 Swagger em /swagger", endereco);
        });

        return app;
    }
}
