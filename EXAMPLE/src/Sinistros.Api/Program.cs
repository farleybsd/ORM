using DDDWithHttp.Orm.DependencyInjection;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Sinistros.Api.Infrastructure.ApolicesApi;
using Sinistros.Api.Infrastructure.Erros;
using Sinistros.Api.Infrastructure.Persistencia;

const string RotaHealth = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetailsRfc9457();
builder.Services.AddFastEndpoints().OpenApiDocument(o => o.DocumentName = "v1");

// ORM com Postgres e SQL Server configurados ao mesmo tempo (seção "Orm");
// o repositório usa o banco escolhido em "Persistencia:Banco".
builder.Services.AddPersistenciaDeSinistros(builder.Configuration);
builder.Services.AddHealthChecks().AddOrmHealthCheck();

builder.Services.AddScoped<Sinistros.Api.Features.RegistrarSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ConsultarSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ListarSinistrosPorApolice.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ConsultarApoliceDoSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.AtualizarSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ExcluirSinistro.Handler>();

builder.Services.AddApolicesApiClient(builder.Configuration);

var app = builder.Build();

app.UseProblemDetailsRfc9457();
app.UseFastEndpoints(c => c.Errors.UseProblemDetailsRfc9457());
app.MapHealthChecks(RotaHealth);
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Sinistros API v1"));

app.Run();

public partial class Program;
