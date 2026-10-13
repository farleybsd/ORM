## Erros comuns

| Erro | Por que é um problema | Como corrigir |
|---|---|---|
| `Detail = exception.Message` no handler global | Vaza informação interna | Texto fixo no `Detail`, exceção só no log |
| Esquecer `UseExceptionHandler()` | Em dev aparece a página de exceção; em produção, corpo vazio | Sempre registrar e testar com `Production` |
| Registrar o handler global antes dos específicos | O global retorna `true` e os específicos nunca rodam | Registrar do mais específico para o mais genérico |
| Colocar dados da ocorrência no `title` | Quebra a regra de título fixo por tipo | Dados vão para `detail` e para extensões |
| Cliente fazendo parse do `detail` | Qualquer ajuste de texto quebra a integração | Expor dados como extensões estruturadas |
| Status escolhido em cada endpoint | Inconsistência entre endpoints | Mapeamento centralizado em `ParaStatusCode` |
| Lançar exceção para "não encontrado" | Custo de desempenho e assinatura que mente | `Resultado<T>` para erros esperados |
| Renomear um `code` publicado | Quebra clientes silenciosamente | Criar novo código e manter o antigo |
| `status` do corpo diferente do status HTTP | Viola a RFC e confunde clientes | Sempre definir o status da resposta e do corpo juntos |

## Checklist final

- [ ] `AddProblemDetails` registrado com `CustomizeProblemDetails`
- [ ] `UseExceptionHandler` e `UseStatusCodePages` no pipeline
- [ ] Handler global que não expõe mensagem nem stack trace
- [ ] Handlers específicos registrados antes do global
- [ ] Erros de domínio em um catálogo, com código, título fixo e metadados
- [ ] Mapeamento de `TipoErro` para status em um único lugar
- [ ] Todo erro com `type`, `code`, `instance` e `traceId`
- [ ] Validação nativa do .NET 10 devolvendo `HttpValidationProblemDetails`
- [ ] Endpoints documentados com `ProducesProblem` e `ProducesValidationProblem`
- [ ] Tabela de códigos de erro publicada para os consumidores
- [ ] Testes de integração em `Production`, incluindo o teste de não vazamento

## Exercícios

1. **Novo erro de negócio.** Crie o erro `pedido-valor-minimo` (422) para pedidos com menos de 3 unidades no total. Inclua a extensão `minimo` e escreva o teste
2. **Cancelamento com prazo.** Um pedido só pode ser cancelado até 7 dias após a criação. Use um `TimeProvider` injetado, devolva `pedido-prazo-cancelamento-expirado` (409) e teste com `FakeTimeProvider`
3. **Requisição cancelada pelo cliente.** Crie um `IExceptionHandler` para `OperationCanceledException` quando `httpContext.RequestAborted.IsCancellationRequested` for `true`. Ele não deve registrar como erro no log. Discuta que status usar, sabendo que o cliente já foi embora
4. **Tradução do título.** Use o header `Accept-Language` para devolver `title` e `detail` em inglês ou português, mantendo `type` e `code` idênticos. Por que isso não quebra o contrato?
5. **Página de documentação.** Faça o `type` apontar para uma página real: crie um endpoint `GET /problemas/{codigo}` que devolve a descrição de cada erro do catálogo

## Resumo

- A RFC 9457 padroniza erros em APIs HTTP com `type`, `title`, `status`, `detail`, `instance` e extensões, servidos como `application/problem+json`
- O ASP.NET Core implementa isso nativamente com `AddProblemDetails`, `UseExceptionHandler` e `UseStatusCodePages`
- Erros esperados são valores (`Resultado<T>` com `Erro`), erros inesperados são exceções
- O domínio descreve o erro com um `TipoErro`; a camada HTTP traduz esse tipo em status em um único lugar
- O handler global registra a exceção completa no log e devolve ao cliente apenas um 500 genérico com `traceId`
- Um contrato estável tem `type` e `code` imutáveis, título fixo, dados em extensões estruturadas e evolução apenas aditiva
- Testes de integração em `Production` garantem o formato e impedem que alguém volte a vazar exceção
