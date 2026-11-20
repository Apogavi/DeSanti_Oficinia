# Backend DeSanti Automotiva

Estado revisado em **29/10/2026**. API ASP.NET Core em .NET 10, com MySQL e
quatro layers orientadas pelos princípios de Clean Architecture.

| Projeto | Responsabilidade atual |
| --- | --- |
| `DeSanti.Domain` | Entities de Cliente, OS e peças; cálculo oficial em centavos; abertura com histórico do cliente e regras de retorno. |
| `DeSanti.Application` | DTOs, validation, Application Services e interfaces de Repositories. |
| `DeSanti.Infrastructure` | EF Core, provider MySQL, mappings e Repositories concretos. |
| `DeSanti.Api` | Controllers, HTTP, ProblemDetails e composition root. |
| `DeSanti.Api.Tests` | Unit Tests de regras e Integration Tests de HTTP com Repositories simulados. |

As dependências continuam `Api → Application + Infrastructure`,
`Infrastructure → Application + Domain` e `Application → Domain`.

## Integração da OS

O fluxo implementado é **selecionar cliente cadastrado → enviar OS → receber ID e
total oficial**. O guia [INTEGRACAO_OS.md](docs/INTEGRACAO_OS.md) explica os arquivos,
o API contract completo, o cálculo, a transaction, os estados da tela e os limites
da verificação. É a referência atual para continuar essa feature.

- `GET /api/v1/clientes`: controller existente, utilizado pelo Flutter.
- `POST /api/v1/ordens-servico`: recebe `ClienteId`, veículo, serviço, peças,
  mão de obra e retorno. Retorna `201` com `{ "id": 42, "totalCentavos": 12303 }`.
- Validation retorna `400` com `errors`; cliente inexistente retorna `404`;
  cliente inativo retorna `409`. Falhas inesperadas usam `ProblemDetails` em `500`.
- A OS copia nome, documento e telefone do cadastro do cliente na abertura.
  Alterar o cadastro depois não altera esses campos históricos.
- O Repository grava OS e peças na mesma transaction. A API não executa migrations
  nem cria tabelas automaticamente ao iniciar.

Não há listagem/detalhe/finalização de OS, PDF ou integração Falcon nesta etapa.

## Dependencies

O provider aprovado é `MySql.EntityFrameworkCore` **10.0.12**, compatível com EF
Core **10.0.12** e .NET 10. Ele substitui o Pomelo 9, que exigia EF Core 9.
`Microsoft.AspNetCore.Mvc.Testing` **10.0.12** fornece o TestServer dos tests.
A referência antiga a `Microsoft.AspNetCore.Identity` 2.3.13 foi removida porque
não havia uso no código; não havia fluxo de authentication implementado.

`MySqlConnectionFactory`/`IDbConnectionFactory` e sua dependency `MySqlConnector`
foram preservados, mas não participam da integração: os Repositories usam EF Core.
O provider oficial utiliza `MySql.Data` internamente. Não criar duas unidades de
persistência para a mesma operação.

Fontes: [provider oficial](https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.12),
[compatibilidade Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#compatibility).

## MySQL de desenvolvimento

Diogo confirmou MySQL local via Docker Compose. O arquivo
[`../compose.dev.yaml`](../compose.dev.yaml) usa MySQL **8.4.13 LTS**, porta
`127.0.0.1:3306`, banco `desanti_dev`, usuário de aplicação `desanti_app` e volume
persistente. O banco não é publicado na LAN e não reinicia automaticamente no boot.

Na raiz do repositório:

```bash
python3 scripts/configure_dev.py
sudo docker compose --env-file .env.local -f compose.dev.yaml up -d --wait
```

O script gera senhas aleatórias em `.env.local`, ignorado pelo Git, com permissão
`600`. Executá-lo novamente preserva o arquivo existente. Não compartilhar esse
arquivo. A API usa o usuário da aplicação, não o usuário root do MySQL.

A imagem executa [schema_criacao_referencia.sql](docs/schema_criacao_referencia.sql)
**somente na primeira inicialização de um volume vazio**. O SQL foi gerado do
modelo EF Core, com `ENGINE=InnoDB` explícito; não é migration incremental para
um banco existente. Não remover o volume para aplicar alterações futuras.
Migrations ou alterações em bancos existentes exigem solicitação específica.

Para parar o banco preservando os dados:

```bash
sudo docker compose --env-file .env.local -f compose.dev.yaml stop
```

## Execução e checks

Na raiz do repositório:

```bash
dotnet restore backend/DeSanti.slnx
dotnet build backend/DeSanti.slnx --no-restore
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj --no-restore
./scripts/dev-api.sh
```

`dev-api.sh` carrega a connection string de `.env.local` e inicia o perfil `http`
em `http://localhost:5085`. O OpenAPI fica em `/openapi/v1.json` em Development.
O SQL inicial e a geração do modelo não inserem clientes fictícios automaticamente.

Os tests automatizados não acessam o MySQL nem executam migrations. Os tests de
HTTP substituem os Repositories por fakes; o teste do provider apenas gera SQL em
memória. Isso não comprova a transaction contra um banco real. O resultado da
verificação e as pendências estão no guia de integração. O check real separado
`./scripts/verify-dev-database.sh` foi executado com autorização em 29/10/2026:
criação, histórico e rollback passaram no MySQL local, e os dados fictícios
foram removidos. Ele não faz parte de `dotnet test`.

## Custo das peças — evolução de 04/11/2026

O backend passou a aceitar custo unitário opcional, separado do preço cobrado.
Consulte [CUSTO_PECAS.md](docs/CUSTO_PECAS.md) para contrato, validation e proposta
de atualização incremental. A coluna já foi adicionada ao MySQL local `desanti_dev`, com autorização de
Diogo e verificação de metadata em 04/11/2026. O frontend também foi adaptado;
a persistência real do custo por POST e o dashboard continuam pendentes.
