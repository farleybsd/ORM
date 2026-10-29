var builder = DistributedApplication.CreateBuilder(args);

// Postgres em container, com volume para manter os dados entre execuções.
// Na primeira inicialização (volume vazio) o container cria o banco "loja"
// e roda os scripts de Database/*.sql nele.
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "loja")
    .WithInitFiles("../My-Orm-postgres.Api/Database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

// O banco já é criado pelo container, então o script de criação do Aspire não faz nada
var loja = postgres.AddDatabase("loja")
    .WithCreationScript("SELECT 1;");

var cache = builder.AddRedis("cache");

// A API lê ConnectionStrings:DefaultConnection, então a referência usa esse nome
var api = builder.AddProject<Projects.My_Orm_postgres_Api>("api")
    .WithReference(loja, "DefaultConnection")
    .WithReference(cache)
    .WaitFor(loja)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Angular (ng serve na porta 4200); o proxy.config.json encaminha /api para a API
builder.AddJavaScriptApp("front", "../front", "start")
    .WithHttpEndpoint(port: 4200, isProxied: false)
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
