using FastEndpoints;
using My_Orm_Data_Access.Core;
using My_Orm_postgres.Api.Extensions;
using My_Orm_postgres.Api.Infrastructure.Erros;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry (logs, traces e métricas para o painel do Aspire) e health checks
builder.AddServiceDefaults();

builder.Services.AddProblemDetailsRfc9457();
builder.Services.AddFastEndpoints();
builder.Services.AddSwaggerDocumentation();

// Uma sessão do ORM por requisição (mesma conexão/transação para todo o request)
builder.Services.AddOrm(o =>
{
    o.UsePostgres(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi encontrada. Confira o appsettings.json"));

    o.SlowQueryThreshold = TimeSpan.FromMilliseconds(
        builder.Configuration.GetValue("Orm:SlowQueryThresholdMs", 500));
});

builder.Services.AddScoped<My_Orm_postgres.Api.Features.CriarCliente.Handler>();
builder.Services.AddScoped<My_Orm_postgres.Api.Features.ConsultarCliente.Handler>();
builder.Services.AddScoped<My_Orm_postgres.Api.Features.ListarClientes.Handler>();
builder.Services.AddScoped<My_Orm_postgres.Api.Features.AtualizarCliente.Handler>();
builder.Services.AddScoped<My_Orm_postgres.Api.Features.ExcluirCliente.Handler>();

var app = builder.Build();

app.UseProblemDetailsRfc9457();
app.UseFastEndpoints(c => c.Errors.UseProblemDetailsRfc9457());
app.UseSwaggerDocumentation();
app.MapDefaultEndpoints();
app.UseStartupBanner();

app.Run();

public partial class Program;
