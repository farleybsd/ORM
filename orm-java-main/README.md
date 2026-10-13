# ORM sem Elefante Branco (edição Java): código-fonte

Código de cada capítulo do ebook, no estado exato em que o capítulo termina. Cada pasta é um projeto Maven completo: entre nela e rode `mvn -q compile exec:java`.

Use este repositório para **conferir** o seu código, não para pular a digitação. Quem digita entende; quem copia esquece.

## Antes de começar

1. Instale o **JDK 17 ou superior**, o **Maven** e o **PostgreSQL** (o MySQL só é necessário no capítulo 16).
2. Crie um banco chamado `loja` e rode o script `sql/postgres.sql` nele.
3. Na raiz do repositório, rode o script de configuração. Na primeira vez ele cria o `.env`; confira a senha do banco nele e rode de novo para copiar o `.env` para todos os capítulos:
   - Windows (PowerShell): `.\configurar.ps1`
   - Linux/Mac: `./configurar.sh`
4. Entre na pasta de um capítulo e rode `mvn -q compile exec:java`.

Prefere a IDE? Abra a pasta do capítulo no IntelliJ IDEA (ele reconhece o `pom.xml`) e rode a classe `meuorm.Main`. Confira se o *working directory* da configuração de execução é a pasta do capítulo, senão o `.env` não é encontrado.

O `.env` nunca vai para o Git (está no `.gitignore`), exatamente como o capítulo 3 ensina.

## Capítulos

| Pasta | Capítulo | Observação |
| --- | --- | --- |
| [`cap02-jdbc`](cap02-jdbc/) | JDBC em 10 minutos | A conexão está no código de propósito (é o "antes"). Troque a senha se a sua for diferente. |
| [`cap03-dotenv`](cap03-dotenv/) | Configuração com .env | Apague uma linha do .env e rode de novo para ver a mensagem de erro. |
| [`cap04-interface`](cap04-interface/) | Interface: o contrato do ORM |  |
| [`cap05-fachada`](cap05-fachada/) | Injeção de dependência por driver | O esperado aqui é a mensagem "Driver 'Postgres' não encontrado". O primeiro driver chega no capítulo 6. |
| [`cap06-driver-postgres`](cap06-driver-postgres/) | O driver Postgres, parte 1 |  |
| [`cap07-sql-injection`](cap07-sql-injection/) | SQL Injection de verdade | Rode depois do capítulo 2 ou 6 para ter clientes na tabela. |
| [`cap08-crud`](cap08-crud/) | CRUD no driver |  |
| [`cap09-transacoes`](cap09-transacoes/) | Transações | A mensagem do rollback ainda vem crua, em inglês, direto do PostgreSQL. No capítulo 10 ela vira uma mensagem amigável. |
| [`cap10-erros`](cap10-erros/) | Erros que o dev entende |  |
| [`cap11-reflection`](cap11-reflection/) | Reflection sem medo |  |
| [`cap12-model`](cap12-model/) | Load e Save automáticos |  |
| [`cap13-dirty-check`](cap13-dirty-check/) | Dirty-check |  |
| [`cap14-trava-otimista`](cap14-trava-otimista/) | Trava otimista com updated_at |  |
| [`cap15-anotacoes`](cap15-anotacoes/) | Anotações @CalcField e @Table |  |
| [`cap16-mysql`](cap16-mysql/) | Segundo driver: MySQL | Mesmo Main.java do capítulo 15. Rode com o .env do Postgres e depois troque pelo conteúdo de .env.mysql.example. |
| [`cap17-projeto-final`](cap17-projeto-final/) | Projeto final: CRUD de clientes |  |

Os capítulos 1 e 18 são só teoria e não têm código.

## Diferenças em relação ao livro

Nos exemplos que gravam clientes, o e-mail leva um código aleatório (`maria-3f9a...@email.com`) em vez de um endereço fixo. Assim você pode rodar o mesmo capítulo várias vezes sem bater na restrição de e-mail único. Fora isso, o código é o do livro.

## Problemas comuns

**"A variável 'ORM_DRIVER' não foi encontrada"**: falta o `.env` na pasta do capítulo, ou o programa está rodando a partir de outra pasta. Rode o script de configuração e execute de dentro da pasta do capítulo.

**"Driver 'Postgres' não encontrado"**: no capítulo 5 isso é esperado. Em outros, confira se escreveu `Postgres` com P maiúsculo no `.env`.

**"No suitable driver found for jdbc:..."**: o driver JDBC não está no classpath. Rode pelo Maven (`mvn -q compile exec:java`), que baixa a dependência do `pom.xml`.

**"Public Key Retrieval is not allowed" (MySQL)**: acontece com o usuário autenticado por `caching_sha2_password` sem SSL. Para desenvolvimento local, acrescente `?allowPublicKeyRetrieval=true` ao final da URL no `MySql.java`.

**Acentos aparecendo como `?` no console do Windows**: rode `chcp 65001` no terminal antes de executar.

## Licença

Este código é de uso pessoal de quem adquiriu o ebook. Veja [LICENSE.md](LICENSE.md).
