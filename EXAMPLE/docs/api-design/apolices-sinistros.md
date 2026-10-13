# Apólices & Sinistros — Padrões de API (leitura do MDSL)

> Versão legível de [`apolices-sinistros.mdsl`](./apolices-sinistros.mdsl). Abra este
> arquivo com `Ctrl+Shift+V` no VS Code para o preview renderizado. O `.mdsl` é a
> fonte "oficial" (independente de protocolo); este `.md` é só uma projeção dele
> para leitura — se o `.mdsl` mudar, atualize esta tabela manualmente.

## Legenda dos padrões (Microservice API Patterns)

**Papel do endpoint (Endpoint Role)**

| Padrão | Significado |
|---|---|
| Information Holder Resource | Endpoint que expõe e gerencia um conjunto de dados coeso. |
| ↳ Master Data Holder | Especialização: dados de referência/contrato, relativamente estáveis (ex.: a apólice em si). |
| ↳ Activity Log Resource | Especialização: registro de eventos/atividades que aconteceram, com ciclo de vida próprio (ex.: um sinistro). |
| Link Lookup Resource | Endpoint dedicado a resolver uma referência cruzada para outro recurso/serviço. |

**Responsabilidade da operação (Operation Responsibility)**

| Padrão | Significado |
|---|---|
| Retrieval Operation | Consulta, sem efeito colateral. |
| State Creation Operation | Cria um novo recurso/registro. |

**Tipo de elemento do payload**

| Padrão | Significado |
|---|---|
| ID Element | Identificador do recurso. |
| Atomic Parameter | Campo escalar simples. |
| Parameter Tree | Objeto estruturado/aninhado. |
| Atomic Parameter List | Coleção (`*`) de valores ou Parameter Trees. |
| Link Element | Campo que referencia outro recurso/API (chave estrangeira cross-service). |
| Metadata Element | Campo de auditoria/controle atribuído pelo servidor (ex.: timestamp). |
| Error Report | Payload padronizado de erro. |

---

## API de Apólices (upstream / provedora)

### Endpoint: `ApoliceInformationHolder`
**Papel:** Information Holder Resource › *Master Data Holder*

| Operação | Responsabilidade | Entrada | Saída |
|---|---|---|---|
| `consultarApolice` | Retrieval Operation | `ApoliceId` (ID Element) | `Apolice` |
| `listarApolices` | Retrieval Operation | — | `Apolice*` (Atomic Parameter List) |

### Data type: `Apolice` (Parameter Tree)

| Campo | Tipo MDSL | Padrão |
|---|---|---|
| `id` | `ApoliceId` | ID Element |
| `numero` | `D<string>` | Atomic Parameter (chave de negócio) |
| `segurado` | `Segurado` | Parameter Tree |
| `vigencia` | `Vigencia` | Parameter Tree |
| `coberturas` | `Cobertura*` | Atomic Parameter List de Parameter Trees |

`Segurado { nome, documento }` e `Vigencia { inicio, fim }` são Parameter Trees simples;
`Cobertura { tipo, limiteIndenizacao }` também.

---

## API de Sinistros (downstream / consumidora da API de Apólices)

### Endpoint: `SinistroActivityLog`
**Papel:** Information Holder Resource › *Activity Log Resource*

| Operação | Responsabilidade | Entrada | Saída |
|---|---|---|---|
| `registrarSinistro` | State Creation Operation | `NovoSinistro` | `Sinistro` |
| `consultarSinistro` | Retrieval Operation | `SinistroId` (ID Element) | `Sinistro` |
| `listarSinistrosPorApolice` | Retrieval Operation | `ApoliceId` (Link Element, usado como filtro) | `Sinistro*` |

### Endpoint: `SinistroApoliceLinkLookup`
**Papel:** Link Lookup Resource — resolve a referência cruzada que um `Sinistro` mantém com a API de Apólices.

| Operação | Responsabilidade | Entrada | Saída |
|---|---|---|---|
| `consultarApoliceDoSinistro` | Retrieval Operation | `SinistroId` | `ApoliceDoSinistro` |

Internamente delega para `ApoliceInformationHolder.consultarApolice` (chamada
backend-to-backend) — ver diagrama abaixo.

### Data type: `Sinistro` (Parameter Tree)

| Campo | Tipo MDSL | Padrão |
|---|---|---|
| `id` | `SinistroId` | ID Element |
| `apoliceId` | `ApoliceId` | **Link Element** para `ApoliceInformationHolder` |
| `dataOcorrencia` | `D<date>` | Atomic Parameter |
| `tipoCobertura` | `TipoCobertura` | Atomic Parameter |
| `descricao` | `D<string>` | Atomic Parameter |
| `status` | `D<string>` | Atomic Parameter (enum: `Registrado`) |
| `dataRegistro` | `MD<dateTime>` | **Metadata Element** (timestamp de auditoria) |

### Data type: `ApoliceDoSinistro` (Parameter Tree)

| Campo | Tipo MDSL | Padrão |
|---|---|---|
| `sinistroId`, `apoliceId` | `SinistroId`, `ApoliceId` | ID Element / Link Element |
| `dataOcorrencia`, `vigenciaInicio`, `vigenciaFim` | `D<date>` | Atomic Parameter |
| `tipoCobertura` | `TipoCobertura` | Atomic Parameter |
| `coberturas` | `D<string>*` | Atomic Parameter List |
| `vigenteNaDataOcorrencia`, `possuiCoberturaDoSinistro` | `D<boolean>` | Atomic Parameter — **conclusões calculadas por Sinistros**, não vêm prontas da API externa |

---

## Como as duas APIs se relacionam

```
┌─────────────────┐        Link Lookup Resource         ┌──────────────────────┐
│  Sinistros API   │  consultarApoliceDoSinistro(id) →   │    Apolices API       │
│                   │ ───────────────────────────────────▶│                        │
│ SinistroActivityLog│  (chamada backend-to-backend a     │ ApoliceInformationHolder│
│ SinistroApoliceLink│   consultarApolice(apoliceId))     │  (Master Data Holder)  │
└─────────────────┘                                      └──────────────────────┘
```

Bindings HTTP concretos (OpenAPI 3.1) desses mesmos contratos:
[`apolices-openapi.yaml`](./apolices-openapi.yaml) e
[`sinistros-openapi.yaml`](./sinistros-openapi.yaml).
