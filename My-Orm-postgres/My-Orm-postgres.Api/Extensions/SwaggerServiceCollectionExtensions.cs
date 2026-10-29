using FastEndpoints.OpenApi;

namespace My_Orm_postgres.Api.Extensions;

public static class SwaggerServiceCollectionExtensions
{
    public const string DocumentName = "v1";

    /// <summary>
    /// Registra o documento OpenAPI gerado pelo FastEndpoints a partir dos endpoints e dos
    /// <c>Summary</c> de cada feature.
    /// </summary>
    /// <remarks>
    /// Deve ser chamado depois de <c>AddFastEndpoints</c>. Use em conjunto com
    /// <see cref="SwaggerApplicationBuilderExtensions.UseSwaggerDocumentation"/>.
    /// </remarks>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.OpenApiDocument(o =>
        {
            o.DocumentName = DocumentName;
            o.Title = "Loja API";
            o.Version = DocumentName;
        });

        return services;
    }
}
