namespace My_Orm_postgres.Api.Extensions;

public static class SwaggerApplicationBuilderExtensions
{
    /// <summary>
    /// Publica o documento OpenAPI em <c>/openapi/v1.json</c> e a interface do Swagger em
    /// <c>/swagger</c>.
    /// </summary>
    /// <remarks>
    /// Deve ser chamado depois de <c>UseFastEndpoints</c>.
    /// </remarks>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        string documento = SwaggerServiceCollectionExtensions.DocumentName;

        app.MapOpenApi();
        app.UseSwaggerUI(o => o.SwaggerEndpoint($"/openapi/{documento}.json", $"Loja API {documento}"));

        return app;
    }
}
