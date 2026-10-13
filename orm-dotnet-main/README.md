# ORM sem Elefante Branco (edição C#/.NET): código-fonte

Código de cada capítulo do ebook, no estado exato em que o capítulo termina. Cada pasta é um projeto completo: entre nela e rode `dotnet run`.

Use este repositório para **conferir** o seu código, não para pular a digitação. Quem digita entende; quem copia esquece.

## Antes de começar

1. Instale o **.NET 8 ou superior** e o **PostgreSQL** (o MySQL só é necessário no capítulo 16).
2. Crie um banco chamado `loja` e rode o script `sql/postgres.sql` nele.
3. Na raiz do repositório, rode o script de configuração. Na primeira vez ele cria o `.env`; confira a senha do banco nele e rode de novo para copiar o `.env` para todos os capítulos:
   - Windows (PowerShell): `.\configurar.ps1`
   - Linux/Mac: `./configurar.sh`
4. Entre na pasta de um capítulo e rode `dotnet run`.

O `.env` nunca vai para o Git (está no `.gitignore`), exatamente como o capítulo 3 ensina.

## Capítulos

| Pasta | Capítulo | Observação |
| --- | --- | --- |
| [`cap02-adonet`](cap02-adonet/) | ADO.NET em 10 minutos | A string de conexão está no código de propósito (é o "antes"). Troque a senha se a sua for diferente. |
| [`cap03-dotenv`](cap03-dotenv/) | Configuração com .env | Apague uma linha do .env e rode de novo para ver a mensagem de erro. |
| [`cap04-interface`](cap04-interface/) | Interface: o contrato do ORM |  |
| [`cap05-fachada`](cap05-fachada/) | Injeção de dependência por driver | O esperado aqui é a mensagem "Driver 'Postgres' não encontrado". O primeiro driver chega no capítulo 6. |
| [`cap06-driver-postgres`](cap06-driver-postgres/) | O driver Postgres, parte 1 | O aviso CS0649 sobre o campo transaction é esperado nesta etapa. Ele some no capítulo 9. |
| [`cap07-sql-injection`](cap07-sql-injection/) | SQL Injection de verdade | Rode depois do capítulo 2 ou 6 para ter clientes na tabela. |
| [`cap08-crud`](cap08-crud/) | CRUD no driver |  |
| [`cap09-transacoes`](cap09-transacoes/) | Transações | A mensagem do rollback ainda vem crua, em inglês, direto do PostgreSQL. No capítulo 10 ela vira uma mensagem amigável. |
| [`cap10-erros`](cap10-erros/) | Erros que o dev entende |  |
| [`cap11-reflection`](cap11-reflection/) | Reflection sem medo |  |
| [`cap12-model`](cap12-model/) | Load e Save automáticos |  |
| [`cap13-dirty-check`](cap13-dirty-check/) | Dirty-check |  |
| [`cap14-trava-otimista`](cap14-trava-otimista/) | Trava otimista com updated_at |  |
| [`cap15-atributos`](cap15-atributos/) | Atributos customizados |  |
| [`cap16-mysql`](cap16-mysql/) | Segundo driver: MySQL | Mesmo Program.cs do capítulo 15. Rode com o .env do Postgres e depois troque pelo conteúdo de .env.mysql.example. |
| [`cap17-projeto-final`](cap17-projeto-final/) | Projeto final: CRUD de clientes |  |

Os capítulos 1 e 18 são só teoria e não têm código.

## Diferenças em relação ao livro

Nos exemplos que gravam clientes, o e-mail leva um código aleatório (`maria-3f9a...@email.com`) em vez de um endereço fixo. Assim você pode rodar o mesmo capítulo várias vezes sem bater na restrição de e-mail único. Fora isso, o código é o do livro.

## Problemas comuns

**"A variável 'ORM_DRIVER' não foi encontrada"**: falta o `.env` na pasta do capítulo. Rode o script de configuração.

**"Driver 'Postgres' não encontrado"**: no capítulo 5 isso é esperado. Em outros, confira se escreveu `Postgres` com P maiúsculo no `.env`.

**Falha de conexão com o banco**: confira usuário, senha e porta no `.env`, e se o PostgreSQL está rodando.

**Rodando pelo Visual Studio**: o `.csproj` de cada capítulo já copia o `.env` para a pasta de saída (veja o capítulo 3).

## Licença

Este código é de uso pessoal de quem adquiriu o ebook. Veja [LICENSE.md](LICENSE.md).
