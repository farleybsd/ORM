# Traefik — domínios locais da loja

Proxy reverso que publica as aplicações em domínios locais:

| Domínio | Aplicação | Destino |
|---|---|---|
| http://front.loja.localhost | Front Angular (`ng serve`) | `host.docker.internal:4200` |
| http://api.loja.localhost | API C# | `host.docker.internal:5101` |
| http://localhost:8080 | Painel do Traefik | — |

O Traefik roda em container; o front e a API rodam na sua máquina (via `aspire run`).

## Arquivos

```
traefik/
├── docker-compose.yml   # container do Traefik (portas 80 e 8080)
├── traefik.yml          # configuração estática (entrypoint, painel, provider de arquivo)
└── dynamic/
    └── loja.yml         # rotas: domínio -> aplicação
```

## Pré-requisitos

- Docker Desktop rodando
- Portas **80** e **8080** livres

> Não precisa editar o arquivo `hosts`: Chrome, Edge e Firefox resolvem
> qualquer `*.localhost` para `127.0.0.1` automaticamente.

## 1. Subir as aplicações

Na raiz do repositório (`C:\Git\ORM\My-Orm-postgres`):

```powershell
aspire run
```

Isso sobe Postgres, Redis, a API (porta 5101) e o front (porta 4200).

## 2. Subir o Traefik

```powershell
cd C:\Git\ORM\My-Orm-postgres\traefik
docker compose up -d
```

Acesse:

- http://front.loja.localhost
- http://api.loja.localhost/swagger
- http://localhost:8080 (painel do Traefik)

## Comandos úteis

```powershell
docker compose ps          # status do container
docker compose logs -f     # logs (inclui o access log de cada requisição)
docker compose restart     # reiniciar
docker compose down        # parar e remover o container
```

Depois de alterar `dynamic/loja.yml`, rode `docker compose restart`: no
Windows o Docker nem sempre avisa o Traefik que o arquivo mudou.

## Problemas comuns

**502 Bad Gateway** — a aplicação não está rodando ou não está acessível para o Docker.

- Confirme que o `aspire run` está de pé e que `http://localhost:4200` e
  `http://localhost:5101/swagger` abrem.
- O Docker chega na sua máquina por IPv4 (`127.0.0.1`). Por isso o
  `front/angular.json` configura o `ng serve` com `"host": "127.0.0.1"`.
  Se houver outro `ng serve` antigo aberto em outro terminal, feche-o.

**"Blocked request. This host is not allowed"** no front — o domínio precisa
estar em `allowedHosts` no `front/angular.json` (já está configurado para
`front.loja.localhost`).

**404 page not found** — o domínio digitado não bate com nenhuma regra em
`dynamic/loja.yml`.

**`DNS_PROBE_FINISHED_NXDOMAIN`** — confira se o endereço termina em
`.localhost`. Alguns programas fora do navegador (como `curl` antigo) não
resolvem `*.localhost`; nesse caso use `curl --resolve front.loja.localhost:80:127.0.0.1 ...`.

**Porta 80 em uso** — pare o serviço que está usando a porta (IIS, por exemplo)
ou troque `"80:80"` por `"8081:80"` no `docker-compose.yml` e acesse
`http://front.loja.localhost:8081`.
