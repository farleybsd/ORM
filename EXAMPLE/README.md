# Seguros — DDD + Vertical Slice + FastEndpoints (projeto didático)

Aplicação didática em C#/.NET demonstrando **Domain-Driven Design** e **Vertical Slice
Architecture** com dois contextos delimitados (bounded contexts) do ramo de seguros que se
comunicam **de verdade por HTTP**: **Apólices** e **Sinistros**.

> Este projeto tem fins de aprendizado. As regras de negócio são simplificadas de propósito —
> veja a seção [Simplificações](#simplificações-assumidas) no final deste documento.

## Ambiente

- SDK: **.NET 10.0.301** (`net10.0`), a versão estável mais recente disponível no momento da
  implementação (sem previews).
- `FastEndpoints` 8.3.0 para os endpoints HTTP.
- `Microsoft.Extensions.Http.Resilience` 10.9.0 para timeout, retry e circuit breaker.
- `FastEndpoints.OpenApi` 8.3.0 (geração do documento OpenAPI) + `Swashbuckle.AspNetCore.SwaggerUI`
  10.2.3 (apenas a UI do Swagger, servindo o documento gerado pelo FastEndpoints) em cada API.

## Estrutura

```
DDDWithHttp.sln
DDDWithHttp.slnLaunch.json     configuração de múltiplos projetos de inicialização (Visual Studio)
src/
  Apolices.Api/                 contexto "Apólices" — porta 5201
    Domain/                     Apolice (aggregate), Segurado, PeriodoVigencia, Cobertura,
                                 TipoCobertura, IApoliceRepository — sem qualquer dependência HTTP
    Infrastructure/              InMemoryApoliceRepository (dados fictícios, seed fixo)
    Contracts/                   DTOs do contrato HTTP público (ApoliceResponse e afins)
    Features/
      ConsultarApolice/          Endpoint + Handler (GET /apolices/{id})
      ListarApolices/             Endpoint + Handler (GET /apolices)
    Common/Result.cs             Result/Result<T> minimalista, local a este projeto
  Sinistros.Api/                contexto "Sinistros" — porta 5301
    Domain/                      Sinistro (aggregate), StatusSinistro, TipoCobertura (vocabulário
                                  PRÓPRIO deste contexto), ApoliceReferenciada (VO traduzido do
                                  DTO externo), ISinistroRepository, e a "porta" IApolicesApiClient
                                  + ApoliceConsultaResultado (contrato, sem HTTP)
    Infrastructure/
      InMemorySinistroRepository.cs
      ApolicesApi/                ApolicesApiClient (adaptador HTTP real da porta acima),
                                   ApoliceApiDto (espelha o JSON da API de Apólices),
                                   ApolicesApiOptions (bind de configuração),
                                   ApolicesApiClientServiceCollectionExtensions (registra o
                                   HttpClient tipado + pipeline de resiliência em Program.cs)
    Contracts/                    SinistroResponse
    Features/
      RegistrarSinistro/          Request + Validator + Handler + Endpoint (POST /sinistros)
      ConsultarSinistro/          Endpoint + Handler (GET /sinistros/{id})
      ListarSinistrosPorApolice/  Endpoint + Handler (GET /apolices/{apoliceId}/sinistros)
      ConsultarApoliceDoSinistro/ Endpoint + Handler (GET /sinistros/{id}/apolice) — lê o sinistro
                                  local e consulta a apólice vinculada na API de Apólices
    Contracts/ApoliceDoSinistroResponse.cs
    appsettings.json              padrões comuns (sem endereço fixo da API de Apólices)
    appsettings.Development.json  BaseUrl = http://localhost:5201
    appsettings.QAS.json          BaseUrl do ambiente de QAS
    appsettings.Production.json   BaseUrl do ambiente de produção
    Common/Result.cs
tests/
  Apolices.Domain.Tests/         regras de PeriodoVigencia e Apolice
  Sinistros.Domain.Tests/        regras de Sinistro.Registrar
  Sinistros.Api.IntegrationTests/ WebApplicationFactory + handler HTTP "roteirizado" (stub) que
                                   substitui o handler primário do HttpClient tipado, simulando
                                   sucesso, 404, falta de cobertura, falhas transitórias (retry),
                                   indisponibilidade persistente (circuit breaker) e timeout
http/
  Apolices.Api.http
  Sinistros.Api.http
```

A organização segue duas referências indicadas para este exercício: a estrutura geral de pastas
por funcionalidade (pastas simples, sem camadas profundas) vem do workshop *FinanceManager -
Vertical Slice*; o agrupamento de arquivos dentro de cada caso de uso (`Endpoint`, `Request`,
`Response`/`Response` compartilhado em `Contracts`, `Validator`, `Handler`) vem do workshop
*Vertical Slice Architecture* (GymErp). Onde as duas divergiam, prevaleceu a organização de casos
de uso da segunda, conforme solicitado.

**Fronteiras de contexto:** nenhuma entidade, value object ou repositório é compartilhado entre
`Apolices.Api` e `Sinistros.Api`. Cada um define seu próprio enum `TipoCobertura` — a tradução
entre os dois vocabulários acontece inteiramente dentro de `Sinistros.Api.Infrastructure.ApolicesApi`
(um DTO cru → `ApoliceReferenciada`, o conceito usado pelo domínio de Sinistros). O tipo `Result`
também é duplicado (não compartilhado) propositalmente em cada projeto, para não criar
acoplamento via "shared kernel" por causa de um utilitário tão pequeno.

## Fluxo de negócio

1. **Apólices** expõe consultas sobre apólices existentes (identificador, segurado, período de
   vigência e coberturas contratadas).
2. **Sinistros** recebe o registro de um sinistro vinculado a uma apólice. Antes de gravar,
   `Sinistros.Api` consulta `Apolices.Api` **via HTTP real** (`GET /apolices/{id}`) para verificar:
   - que a apólice existe (senão, `404`);
   - que ela estava vigente na data da ocorrência (senão, `422`);
   - que possui a cobertura solicitada (senão, `422`).
3. **Sinistros** também expõe `GET /sinistros/{id}/apolice`: lê o sinistro no seu próprio
   repositório e busca a apólice vinculada **ao vivo** em `Apolices.Api` (`GET /apolices/{id}`),
   devolvendo vigência e coberturas já traduzidas para o vocabulário de Sinistros, mais duas
   conclusões do domínio (se a apólice estava vigente na data da ocorrência e se possui a
   cobertura do sinistro). Este contexto guarda apenas o `ApoliceId` — nunca uma cópia dos dados
   do contexto de Apólices, que poderia ficar desatualizada.
4. A comunicação é **unidirecional**: Sinistros conhece Apólices; o contrário nunca acontece.
5. Se a API de Apólices estiver indisponível (mesmo após timeout/retry/circuit breaker), o
   sinistro **não é registrado** — a API responde `503` com uma mensagem clara.

## Resiliência (Sinistros → Apólices)

Configurada em `Sinistros.Api/Program.cs` com `IHttpClientFactory` + cliente tipado
(`IApolicesApiClient` / `ApolicesApiClient`) e `Microsoft.Extensions.Http.Resilience`
(`AddResilienceHandler`), com os parâmetros vindos de `appsettings.json` (seção `ApolicesApi`):

```json
"ApolicesApi": {
  "BaseUrl": "http://localhost:5201",   // vem do appsettings do ambiente — veja a seção abaixo
  "Timeout": "00:00:02",
  "Retry": { "MaxRetryAttempts": 3, "BaseDelay": "00:00:00.500" },
  "CircuitBreaker": {
    "FailureRatio": 0.5, "SamplingDuration": "00:00:10",
    "MinimumThroughput": 4, "BreakDuration": "00:00:15"
  }
}
```

Ordem do pipeline (mais externo → mais interno): **Retry → Circuit Breaker → Timeout por
tentativa**. Isso significa que cada tentativa de retry passa pelo circuit breaker e tem seu
próprio timeout individual.

- **Timeout**: limita quanto tempo cada tentativa pode levar.
- **Retry**: backoff exponencial com jitter (`HttpRetryStrategyOptions`, que já vem com esses
  valores por padrão), limitado a falhas transitórias — `HttpClientResiliencePredicates.IsTransient`
  reconhece `HttpRequestException`, `TimeoutRejectedException` e respostas HTTP 5xx/408/429. Como
  a única chamada feita ao serviço externo é um `GET` (consulta), repeti-la é sempre seguro; não há
  escrita nessa chamada, então nenhuma estratégia de idempotência adicional é necessária.
- **Circuit breaker**: abre depois que a proporção de falhas ultrapassa `FailureRatio` dentro da
  janela `SamplingDuration`, evitando martelar uma API já fora do ar; volta a fechar (via estado
  meio-aberto) depois de `BreakDuration`.
- Respostas de negócio (404 — apólice não existe) **não** entram no retry: são tratadas
  imediatamente como uma resposta de negócio, não como falha transitória.
- Todas as chamadas propagam o `CancellationToken` do endpoint.
- O cliente (`ApolicesApiClient`) loga tentativas/falhas via `ILogger` (sem dados sensíveis — só
  identificadores) e diferencia "apólice não encontrada" de "serviço indisponível"
  (`ApoliceConsultaResultado.NaoEncontrada` vs. `Indisponivel`), permitindo que o caso de uso
  responda com o código HTTP correto (404 vs. 503).

## Endereço da API de Apólices por ambiente

O endereço da API de Apólices **não é fixo no código**: `ApolicesApiOptions.BaseUrl` é resolvido
em tempo de execução pela cadeia de configuração do ASP.NET Core, na ordem

`appsettings.json` → `appsettings.{ASPNETCORE_ENVIRONMENT}.json` → variáveis de ambiente →
argumentos de linha de comando

(cada etapa sobrescreve a anterior). O mesmo binário/imagem, portanto, aponta para um endereço
diferente em cada ambiente, sem recompilação:

| `ASPNETCORE_ENVIRONMENT` | Arquivo                       | `ApolicesApi:BaseUrl`                   |
|--------------------------|-------------------------------|------------------------------------------|
| `Development` (padrão do `launchSettings.json`) | `appsettings.Development.json` | `http://localhost:5201`         |
| `QAS`                    | `appsettings.QAS.json`        | `https://apolices.qas.seguros.example/`  |
| `Production`             | `appsettings.Production.json` | `https://apolices.seguros.example/`      |

> Os endereços de QAS e produção são **placeholders** deste projeto didático — troque-os pelos
> endereços reais, ou sobrescreva-os no deploy (veja abaixo).

O `appsettings.json` base deixa `BaseUrl` vazio de propósito: o endereço é responsabilidade do
ambiente. Para acrescentar um ambiente novo (`Staging`, `HML`, …), basta criar
`appsettings.{Nome}.json` com a seção `ApolicesApi` e subir a aplicação com
`ASPNETCORE_ENVIRONMENT={Nome}`.

**Sobrescrita sem novo build** (o que normalmente se usa em QAS/PRD, via variável de ambiente do
contêiner, do App Service ou do Kubernetes):

```bash
# Linux/contêiner
ASPNETCORE_ENVIRONMENT=QAS ApolicesApi__BaseUrl=https://apolices.qas.interno/ dotnet Sinistros.Api.dll

# PowerShell (Windows)
$env:ASPNETCORE_ENVIRONMENT = "QAS"; $env:ApolicesApi__BaseUrl = "https://apolices.qas.interno/"
dotnet run --project src/Sinistros.Api --no-launch-profile
```

(o duplo sublinhado `__` é o separador de seção usado por variáveis de ambiente.)

**Falha rápida:** o binding é validado com `ValidateOnStart()` — se o ambiente subir sem um
`BaseUrl` http(s) absoluto, a aplicação **não inicia** e a mensagem diz exatamente o que falta,
em vez de só quebrar na primeira requisição:

```
OptionsValidationException: 'ApolicesApi:BaseUrl' precisa ser uma URL http(s) absoluta.
Defina-a no appsettings.{ASPNETCORE_ENVIRONMENT}.json do ambiente ou na variável de ambiente
'ApolicesApi__BaseUrl'.
```

**Endereço com caminho:** o `BaseUrl` recebe uma barra final automaticamente e as rotas do cliente
são relativas (`apolices/{id}`, sem barra inicial). Assim um endereço atrás de gateway como
`https://gateway.qas.interno/apolices-api` gera
`https://gateway.qas.interno/apolices-api/apolices/{id}` — e não descarta o caminho do gateway.

## Erros (RFC 9457 — Problem Details)

As duas APIs respondem **todo** erro como `application/problem+json`, no formato da
[RFC 9457](https://www.rfc-editor.org/info/rfc9457/) (`type`, `title`, `status`, `detail`,
`instance` + extensões `code` e `traceId`). A configuração fica em extensões, em
`Infrastructure/Erros/` de cada API:

```csharp
builder.Services.AddProblemDetailsRfc9457();                    // AddProblemDetails + CustomizeProblemDetails + GlobalExceptionHandler
app.UseProblemDetailsRfc9457();                                 // UseExceptionHandler + UseStatusCodePages
app.UseFastEndpoints(c => c.Errors.UseProblemDetailsRfc9457()); // validação -> HttpValidationProblemDetails
```

- **Erros esperados** (negócio) são valores: `Result<T>` carregando um `Erro` (`Common/Erro.cs`)
  com `Codigo`, `Titulo` fixo, `Detalhe`, `TipoErro` e `Metadados`. Ficam em catálogos
  (`Domain/ErrosDeApolice.cs`, `Domain/ErrosDeSinistro.cs`) e viram ProblemDetails com
  `erro.ParaProblema()`. O status sai de um único lugar: `ErroParaHttp.ParaStatusCode`.
- **Erros inesperados** (exceções) caem no `GlobalExceptionHandler`: a exceção completa vai para o
  log e o cliente recebe só um 500 genérico (`code: erro-interno`) com o `traceId`.
- `type` e `code` nunca mudam depois de publicados; mudou o significado, crie um código novo.

| `code` | Status | API | Extensões |
|---|---|---|---|
| `validacao` | 400 | ambas | `errors` (campo → mensagens) |
| `apolice-nao-encontrada` | 404 | ambas | `apoliceId` |
| `sinistro-nao-encontrado` | 404 | Sinistros | `sinistroId` |
| `apolice-fora-de-vigencia` | 422 | Sinistros | `dataOcorrencia`, `vigenciaInicio`, `vigenciaFim` |
| `cobertura-nao-contratada` | 422 | Sinistros | `tipoCobertura` |
| `sinistro-descricao-obrigatoria` | 400 | Sinistros | — |
| `apolices-api-indisponivel` | 503 | Sinistros | — |
| `apolice-numero-obrigatorio` | 400 | Apólices (domínio) | — |
| `apolice-sem-cobertura` | 422 | Apólices (domínio) | — |
| `erro-interno` | 500 | ambas | — |

O `type` é sempre `https://seguros.example/problemas/{code}`. Exemplo:

```json
{
  "type": "https://seguros.example/problemas/cobertura-nao-contratada",
  "title": "Cobertura não contratada",
  "status": 422,
  "detail": "A apólice não possui a cobertura 'Roubo'.",
  "instance": "/sinistros",
  "code": "cobertura-nao-contratada",
  "tipoCobertura": "Roubo",
  "traceId": "00-9f1c...-01"
}
```

## Como executar

**Visual Studio:** abra `DDDWithHttp.sln` e aperte **F5**. A solução já vem configurada para
iniciar os dois projetos ao mesmo tempo (`DDDWithHttp.slnLaunch.json`), cada um abrindo o
navegador direto na tela do Swagger:
- Apólices: http://localhost:5201/swagger
- Sinistros: http://localhost:5301/swagger

Se o Visual Studio pedir para confirmar o perfil de múltiplos projetos de inicialização na
primeira vez, aceite (ou configure manualmente em botão direito na solução → *Configure Startup
Projects* → *Multiple startup projects*, com os dois marcados como *Start*).

**Linha de comando:** em dois terminais separados, a partir da raiz do repositório:

```bash
dotnet run --project src/Apolices.Api    # http://localhost:5201  (Swagger em /swagger)
dotnet run --project src/Sinistros.Api   # http://localhost:5301  (Swagger em /swagger)
```

Use os arquivos em `http/` (compatíveis com a extensão REST Client do VS Code, JetBrains HTTP
Client, etc.) para testar os endpoints, ou `curl`:

```bash
curl http://localhost:5201/apolices

curl -X POST http://localhost:5301/sinistros \
  -H "Content-Type: application/json" \
  -d '{"apoliceId":"11111111-1111-1111-1111-111111111111","dataOcorrencia":"2026-06-01","tipoCobertura":"Colisao","descricao":"Colisão na Av. Paulista"}'

# a resposta traz o "id" do sinistro; use-o para consultar a apólice vinculada:
curl http://localhost:5301/sinistros/{id}/apolice
```

### Dados fictícios (seed em memória, perdidos ao reiniciar)

| Id                                     | Número  | Vigência                | Coberturas                          |
|-----------------------------------------|---------|--------------------------|--------------------------------------|
| `11111111-1111-1111-1111-111111111111`  | AP-0001 | 2026-01-01 a 2026-12-31  | Colisão, Roubo, Incêndio             |
| `22222222-2222-2222-2222-222222222222`  | AP-0002 | 2024-01-01 a 2024-12-31 (**expirada**) | Colisão              |
| `33333333-3333-3333-3333-333333333333`  | AP-0003 | 2026-06-01 a 2027-05-31  | Incêndio, Danos a Terceiros (**sem Colisão/Roubo**) |

## Como rodar os testes

```bash
dotnet test DDDWithHttp.sln
```

- `Apolices.Domain.Tests` e `Sinistros.Domain.Tests`: regras de domínio puras (vigência,
  cobertura, validações de registro de sinistro) — sem HTTP, sem FastEndpoints.
- `Sinistros.Api.IntegrationTests`: sobe `Sinistros.Api` inteira via `WebApplicationFactory` e
  substitui **apenas o handler HTTP primário** do cliente tipado `IApolicesApiClient` por um
  handler de teste programável (`ScriptedHttpMessageHandler`) — o pipeline de resiliência real
  continua ativo por cima dele. Cada teste cria sua própria instância da fábrica para que o
  estado do circuit breaker de um teste nunca vaze para o próximo. Cobre:
  1. sucesso (`201`);
  2. apólice inexistente (`404`);
  3. apólice sem a cobertura solicitada (`422`);
  4. duas falhas transitórias seguidas de sucesso → o retry recupera (`201`, com 3 chamadas
     registradas no handler de teste);
  5. falhas persistentes → o circuito abre e a chamada seguinte falha rápido, sem nova tentativa
     de rede (`503`, número de chamadas ao handler congelado);
  6. resposta mais lenta que o timeout configurado → tratado como indisponibilidade (`503`);
  7. `GET /sinistros/{id}/apolice`: sucesso (`200`), sinistro inexistente (`404`, sem chamar a API
     de Apólices), apólice que sumiu do outro contexto (`404`) e API de Apólices fora do ar
     (`503`);
  8. montagem da URL de consulta a partir do `ApolicesApi:BaseUrl` do ambiente — inclusive com
     endereço contendo caminho (gateway), garantindo que nada fica fixo no código.

## Demonstração reproduzível de retry / timeout / circuit breaker

**Automatizada** (recomendada): os cenários 4, 5 e 6 de `Sinistros.Api.IntegrationTests` (acima)
demonstram exatamente isso de forma determinística, sem depender de temporização manual.

**Manual**, com as duas APIs rodando:
1. Pare a API de Apólices (`Ctrl+C`).
2. Reenvie a última requisição do arquivo `http/Sinistros.Api.http` (seção "Demonstração manual de
   resiliência"). No console da API de Sinistros aparecem logs do Polly (`OnRetry`) até a resposta
   final `503`, sem o sinistro ser registrado.
3. Repita a mesma requisição mais algumas vezes seguidas: depois que a proporção de falhas
   ultrapassa o limiar configurado (`ApolicesApi:CircuitBreaker`), o log passa a mostrar
   `OnCircuitOpened` e as respostas `503` seguintes ficam praticamente instantâneas — o circuito
   está aberto e nem tenta mais a chamada de rede.
4. Suba a API de Apólices novamente; depois de `BreakDuration`, o circuito volta a permitir
   chamadas e o fluxo normal é restabelecido.

Este comportamento foi validado manualmente durante o desenvolvimento: com a API de Apólices
derrubada, `POST /sinistros` respondeu `503` com a mensagem "Tempo limite excedido ao consultar a
API de Apólices" e a consulta subsequente por sinistros da apólice confirmou que nada foi
persistido.

## Simplificações assumidas

- Sem autenticação, banco de dados ou mensageria — repositórios em memória, dados perdidos ao
  reiniciar cada API.
- Regras de apólice/sinistro simplificadas para fins didáticos: sem franquia, sem endosso, sem
  múltiplos segurados por apólice, sem histórico de alterações.
- Comunicação apenas unidirecional (Sinistros → Apólices); Apólices não tem conhecimento de
  Sinistros.
- Ambas as APIs rodam em HTTP puro (sem TLS) para simplificar a execução local.
- `Result`/`Result<T>` é um tipo minimalista duplicado deliberadamente em cada projeto (não é um
  "shared kernel").
- `ConsultarSinistro` e `ListarSinistrosPorApolice` reaproveitam o mesmo `SinistroResponse` (em
  `Contracts/`) e o método de mapeamento de `RegistrarSinistro.Handler`, em vez de duplicar DTOs
  de resposta equivalentes entre casos de uso.
