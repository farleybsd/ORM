using System.Text.Json.Serialization;
using Apolices.Api.Infrastructure.Erros;
using Apolices.Api.Infrastructure.Persistencia;
using DDDWithHttp.Orm.DependencyInjection;
using FastEndpoints;
using FastEndpoints.OpenApi;

const string RotaHealth = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetailsRfc9457();
builder.Services.AddFastEndpoints().OpenApiDocument(o => o.DocumentName = "v1");

// ORM com Postgres e SQL Server configurados ao mesmo tempo (seção "Orm");
// o repositório usa o banco escolhido em "Persistencia:Banco".
builder.Services.AddPersistenciaDeApolices(builder.Configuration);
builder.Services.AddHealthChecks().AddOrmHealthCheck();

builder.Services.AddScoped<Apolices.Api.Features.ConsultarApolice.Handler>();
builder.Services.AddScoped<Apolices.Api.Features.ListarApolices.Handler>();
builder.Services.AddScoped<Apolices.Api.Features.CriarApolice.Handler>();
builder.Services.AddScoped<Apolices.Api.Features.AtualizarApolice.Handler>();
builder.Services.AddScoped<Apolices.Api.Features.ExcluirApolice.Handler>();

var app = builder.Build();

app.UseProblemDetailsRfc9457();
app.UseFastEndpoints(c =>
{
    c.Errors.UseProblemDetailsRfc9457();
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());   // enums trafegam pelo nome
});
app.MapHealthChecks(RotaHealth);
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Apólices API v1"));

app.Run();

public partial class Program;
