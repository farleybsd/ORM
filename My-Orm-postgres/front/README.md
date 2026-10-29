# Front — Cadastro de Clientes

Front-end em **Angular 22 + PrimeNG 22 + Tailwind CSS 4** para o CRUD de clientes da API `My-Orm-postgres.Api`.

## Como rodar

1. Suba o Postgres e a API (porta `5101`):

   ```bash
   cd ../My-Orm-postgres.Api
   dotnet run --launch-profile http
   ```

2. Suba o front (porta `4200`):

   ```bash
   npm install
   npm start
   ```

   Abra http://localhost:4200. As chamadas para `/api/*` passam pelo proxy (`src/proxy.config.json`)
   até `http://localhost:5101` sem o prefixo `/api`, então a API não precisa de CORS.

## Estrutura

```
src/app
├── core/                      # infraestrutura da aplicação (providers, layout, HTTP)
│   ├── provide-core.ts        # HttpClient, PrimeNG (tema Aura), Toast/Confirmation, locale pt-BR
│   ├── http/api-client.service.ts   # fetch + async/await para POST/PUT/DELETE
│   └── layout/                # cabeçalho, <p-toast> e <p-confirmdialog>
├── shared/                    # código reaproveitável entre features
│   ├── cliente/               # interface Cliente, limites de campos e ClientesService
│   ├── dialog/confirmation/   # confirmação antes de ações destrutivas (Promise<boolean>)
│   ├── feedback/              # mensagens de sucesso/erro (toast)
│   └── http/                  # ProblemDetails (RFC 9457) e normalização de erros
└── features/clientes/
    ├── routes.ts              # /clientes, /clientes/novo, /clientes/editar/:id
    └── pages/
        ├── list/              # p-table, busca, exclusão com confirmação
        └── create-or-edit/    # formulário com Signal Forms
```

## Decisões

- **Signals, sem RxJS no código da aplicação**: leituras com `httpResource`, escritas com `fetch` +
  `async/await`, formulários com Signal Forms (`@angular/forms/signals`). O pacote `rxjs` continua
  instalado porque o `HttpClient` e o PrimeNG dependem dele internamente.
- **Validação espelhando a API**: nome obrigatório (até 150), e-mail obrigatório e válido (até 200).
  Erros `400` (`validacao`) e `409` (`cliente-email-ja-cadastrado`) da API aparecem no campo correspondente.
- **Concorrência otimista**: o PUT reenvia a `versao` lida. Em `409 cliente-versao-desatualizada` o
  formulário é recarregado com a versão atual para o usuário revisar e salvar de novo.

## Licença do PrimeNG

A partir da v22 o PrimeNG usa a *PrimeUI License*: sem chave, exibe o selo "Invalid PrimeUI License".
Existe uma Community License gratuita (pessoas físicas, estudantes e empresas pequenas):
https://primeui.dev/licenses/community. Com a chave em mãos, informe-a em `providePrimeNG({ license: '...' })`
no arquivo `src/app/core/provide-core.ts`.

## Scripts

- `npm start` — servidor de desenvolvimento com proxy
- `npm run build` — build de produção em `dist/front`
- `npm test` — testes unitários (Vitest)
