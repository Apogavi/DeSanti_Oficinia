# Integração da ordem de serviço — 29/10/2026

> Evolução em 04/11/2026: o backend acrescentou `custoUnitarioCentavos` opcional
> por peça. Consulte [CUSTO_PECAS.md](CUSTO_PECAS.md). A coluna foi adicionada ao MySQL local
> com autorização em 04/11/2026 e o frontend foi adaptado. A persistência real
> do custo por POST e o dashboard permanecem pendentes. Os resultados de 29/10 abaixo são históricos.

## Escopo e decisões confirmadas

Diogo autorizou a implementação e confirmou:

1. Selecionar um cliente já cadastrado. Cadastro de clientes na tela fica fora desta etapa.
2. Usar `POST /api/v1/ordens-servico` com o DTO atual, incluindo `clienteId`.
3. Retornar `201` com ID e total oficial; `400` para validation, `404` para cliente
   inexistente e `409` para cliente inativo.
4. Preservar uma cópia do nome, documento e telefone do cliente na abertura da OS.
5. Usar `MySql.EntityFrameworkCore` 10.0.12, `package:http` e
   `Microsoft.AspNetCore.Mvc.Testing`.
6. Criar um MySQL local de desenvolvimento com Docker Compose, mantendo decisões
   de produção pendentes. Nenhuma migration deve ser executada automaticamente.

## Caminho de uma criação

```text
NovaOrdemPage → NovaOrdemViewModel → OrdemServicoRepository → ApiService
    → OrdemServicoController → OrdemServicoService
    → IClienteRepository / OrdemServico.Criar / IOrdemServicoRepository
    → OrdemServicoRepository (EF Core/MySQL)
```

A separação permite testar regras, estado e API contract sem depender de widgets
ou de um MySQL em execução.

### Backend: responsabilidades do código

- `DeSanti.Api/Program.cs`: registra `AddApplication()` e `AddInfrastructure()`;
  adiciona `ProblemDetails` e tratamento de exceções. `partial Program` permite
  que os Integration Tests iniciem um TestServer.
- `DeSanti.Api/Controllers/OrdemServicoController.cs`: recebe JSON, delega ao
  Application Service e converte o resultado em HTTP. Não calcula dinheiro nem
  usa o DbContext.
- `DeSanti.Application/OrdemServicoService.cs`: valida o request, consulta o
  cliente, verifica existência/atividade, cria a Entity e chama uma única operação
  de persistência. Dados históricos vêm do cadastro, não do JSON do cliente HTTP.
- `DeSanti.Application/ClienteService.cs`: define `Ativo = true` na criação,
  alinhando o Service ao default já existente no mapping/schema. A verificação
  real detectou que deixar o `bool` em seu valor CLR padrão criava cliente
  inativo. Um Integration Test cobre essa regressão.
- `DeSanti.Application/Validators/OrdensServico/CriarOrdemServicoValidator.cs`:
  valida campos, limites já definidos nos mappings, retorno e overflow do total.
  A validation antiga de CPF foi removida desse validador porque o contrato de
  OS agora contém apenas `clienteId`; regras do cadastro pertencem a Clientes.
- `DeSanti.Domain/OrdemServico.cs`: `Criar` copia os dados históricos e deixa
  conclusão/previsão de retorno vazias. `CalcularTotalCentavos` usa `checked` na
  multiplicação e na soma; valores negativos e quantidades não positivas são
  rejeitados. O total tem setter privado.
- `DeSanti.Infrastructure/Repositories/OrdemServicoRepository.cs`: inicia uma
  transaction, adiciona a OS com suas peças, chama `SaveChangesAsync` e só então
  confirma. Uma exceção antes do commit desfaz a transaction no dispose.
- `DeSanti.Infrastructure/DependencyInjection.cs`: configura `UseMySQL` com o
  provider oficial de EF Core 10, sem `ServerVersion.AutoDetect` nem consulta ao
  banco no startup. Mantém Repositories/DbContext scoped.

A factory não preenche `ordem.Cliente` com uma Entity desconectada: ela registra o
`ClienteId` e os dados históricos. Assim, `Add(os)` não tenta inserir novamente o
cliente existente. Documento nulo no cadastro existente é copiado como string
vazia, pois o mapping de `Clientes.Documento` permite `NULL`.

### Frontend: responsabilidades do código

- `lib/main.dart`: composition root. Cria um `http.Client`, um `ApiService` e os
  Repositories; injeta um ViewModel novo por rota e fecha o Client no dispose do app.
  Após sucesso, mostra ID e total retornados pelo servidor.
- `lib/core/config/app_config.dart`: URL configurável por `--dart-define=API_BASE_URL`.
  Não contém credenciais de banco ou tokens de fornecedores.
- `lib/core/network/api_service.dart`: encapsula HTTP, JSON, timeout e `ApiException`.
  Usa Client injetado e **não** usa `RetryClient`.
- `lib/features/clientes/`: Model tipado e Repository de `GET /api/v1/clientes`.
  O ViewModel oferece somente clientes ativos para seleção.
- `lib/features/ordens_servico/view_models/nova_ordem_view_model.dart`: mantém
  rascunho, seleção, peças, prévias e estados. Monta o DTO, normaliza a placa,
  valida e aciona o Repository. Não recebe `BuildContext` nem exibe `SnackBar`.
- `lib/features/ordens_servico/nova_ordem_page.dart`: renderiza a tela e mantém
  TextEditingControllers e navegação. `ListenableBuilder` observa o ViewModel;
  todos os campos permanecem montados pelo `SingleChildScrollView`.
- `lib/features/ordens_servico/models/peca_item.dart`: Model separado dos widgets.
  A prévia usa `BigInt` para detectar overflow sem `double` e sem perder centavos.
- `lib/shared/validation/ordem_servico_validation.dart`: validation imediata
  compartilhada com o diálogo de peças. Quantidades e prazo respeitam o tipo
  `Int32` do contrato; o total respeita `Int64`.
- `lib/shared/formatters/money_input_formatter.dart`: mantém a máscara de centavos
  e centraliza também a formatação das prévias com `BigInt`.

O ViewModel pertence à rota e é descartado pela View. Respostas tardias após o
`dispose` não notificam listeners. Os Repositories de API convertem respostas
inválidas em erros compreensíveis; a View não acessa HTTP diretamente.

## API contract

### Seleção de cliente

`GET /api/v1/clientes` é o endpoint existente. Retorna uma lista com campos como:

```json
[
  {
    "id": 1,
    "nome": "Cliente Teste",
    "email": "",
    "telefone": "00000000000",
    "documento": "00000000000",
    "dataCriacao": "2026-10-29T12:00:00Z",
    "dataAlteracao": null,
    "ativo": true
  }
]
```

Os identificadores e dados acima são exemplos sintéticos. Não criar um cliente
com esses dados sem selecionar explicitamente o ambiente de desenvolvimento.
A listagem ainda não tem paginação ou busca no servidor.

### Criação de OS

`POST /api/v1/ordens-servico`, `Content-Type: application/json`:

```json
{
  "clienteId": 1,
  "placa": "AAA0A00",
  "marca": "Marca Teste",
  "modelo": "Modelo Teste",
  "cor": "Azul",
  "ano": null,
  "descricaoServico": "Serviço de teste",
  "pecas": [
    { "nome": "Peça teste", "quantidade": 3, "valorUnitarioCentavos": 101 }
  ],
  "maoDeObraCentavos": 12000,
  "necessitaRetorno": false,
  "prazoRetornoDias": null
}
```

| Campo | Tipo e condição |
| --- | --- |
| `clienteId` | `Int64` positivo; cliente existente e ativo |
| `placa` | String normalizada em maiúsculas, sem hífen; formato brasileiro de 7 caracteres |
| `marca`, `modelo` | Strings obrigatórias, até 100 caracteres conforme mappings |
| `cor` | String obrigatória, até 50 caracteres conforme mapping |
| `ano` | `Int32` ou `null`; de 1900 ao ano atual + 1 |
| `descricaoServico` | String obrigatória |
| `pecas` | Lista obrigatória, podendo estar vazia |
| `pecas[].nome` | String obrigatória, até 200 caracteres conforme mapping |
| `pecas[].quantidade` | `Int32` positivo |
| `pecas[].valorUnitarioCentavos` | `Int64` não negativo |
| `maoDeObraCentavos` | `Int64` não negativo |
| `necessitaRetorno` | Boolean |
| `prazoRetornoDias` | `Int32` positivo quando há retorno; `null` quando não há |

O total é `Σ(quantidade × valorUnitarioCentavos) + maoDeObraCentavos`.
Multiplicações e somas devem caber em `Int64` (`9223372036854775807`). O request
não contém total, dados históricos ou datas de criação/conclusão/retorno.

Resposta `201`:

```json
{ "id": 42, "totalCentavos": 12303 }
```

Não é informado um `Location` para um endpoint de detalhe inexistente nesta etapa.

Exemplo de `400` (outros campos padrão de ProblemDetails podem estar presentes):

```json
{
  "title": "Confira os dados da ordem de serviço.",
  "status": 400,
  "errors": { "clienteId": ["Selecione um cliente."] }
}
```

Errors de peças usam caminhos como `pecas[0].quantidade`; overflow usa
`totalCentavos`. JSON malformado, tipos incompatíveis ou campos obrigatórios
nulos também recebem `400` pelo pipeline `[ApiController]`.

`404` e `409` usam `ProblemDetails` com título de cliente não encontrado/inativo.
Falhas inesperadas retornam `500`, sem expor detalhes internos na resposta.

## Estados da tela e resultado incerto

- **Loading de clientes:** mostra progresso; seleção e envio aguardam a consulta.
  Uma falha permite tentar novamente o GET sem apagar os dados do formulário.
- **Envio:** bloqueia edição, nova submissão e saída pela navegação da página.
- **Success:** retorna à tela inicial e apresenta ID/total confirmados pela API.
- **Erro conhecido (`400`/`404`/`409`):** mantém campos/peças e permite correção.
- **Resultado incerto:** timeout, falha de conexão, erro inesperado ou resposta
  incompatível após POST podem ocorrer depois de uma gravação. A tela mantém o
  rascunho em memória, avisa e bloqueia reenvio nessa instância da página.

Ainda não há idempotency key, consulta de OS ou recuperação persistente de
rascunhos. Fechar o app perde o rascunho; abrir outra página não fornece proteção
global contra duplicidade. Uma futura recuperação deve consultar o servidor ou
adotar idempotency acordada. Não adicionar retry automático ao POST.

## Banco local, bootstrap e execução

O `compose.dev.yaml` usa a imagem oficial `mysql:8.4.13`, porta em loopback,
volume persistente e o SQL `schema_criacao_referencia.sql` para inicializar um
banco novo. O script contém tabelas InnoDB e foreign keys: Cliente → OS com
`RESTRICT`, OS → peças com `CASCADE`. Ele não atualiza bancos existentes e não é
executado pelo startup da API. Não há EF migrations geradas nesta implementação.

As tabelas são `Clientes`, `OrdensServico` e `PecasOrdemServico`, conforme os
mappings atuais. Rascunhos SQL antigos com outros nomes não são equivalentes.

Na raiz do repositório:

```bash
python3 scripts/configure_dev.py
sudo docker compose --env-file .env.local -f compose.dev.yaml up -d --wait
./scripts/dev-api.sh
```

`configure_dev.py` não abre conexão nem altera schema: apenas gera configuração
local se ela não existir. O comando Docker faz a inicialização do banco novo.
O shell script da API carrega a configuração sem executar migrations. Não usar
a configuração de development como configuração de produção.

O banco ficou sem os registros sintéticos ao final da verificação. Para o uso
manual, cadastre um cliente pela API existente; o arquivo
`DeSanti.Api/DeSanti.API.http` contém exemplos de consulta/cadastro/criação de OS.
Substitua o ID de exemplo pelo ID retornado pelo cadastro. A tela de cadastro
de clientes continua fora do escopo aprovado desta etapa.

Em outro terminal:

```bash
cd frontend
flutter pub get
flutter run -d linux --dart-define=API_BASE_URL=http://localhost:5085
```

Alterar `API_BASE_URL` exige reiniciar o aplicativo com o novo `--dart-define`.
Windows deve ser compilado e verificado numa máquina Windows. Flutter Web e
acesso pela LAN não foram validados nesta etapa.

## Verification

Comandos automatizados, sem banco real:

```bash
dotnet build backend/DeSanti.slnx --no-restore
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj --no-restore
cd frontend
flutter analyze --no-pub
flutter test --no-pub
```

Os tests cobrem cálculo exato, snapshot do cliente, retorno, overflow, HTTP/JSON,
registro de Application Services, errors, preservação do rascunho, submissão
simultânea/resultado incerto, validação do topo ao rolar e janela estreita.
`MySqlMappingTests` gera SQL em memória com o provider oficial. Os Integration
Tests usam TestServer e fakes, não comprovam persistência ou rollback no MySQL.

A configuração Compose foi validada por `docker compose ... config --quiet`.
O manifesto remoto da imagem `mysql:8.4.13` foi encontrado.

### Resultados observados em 29/10/2026

- Build da solução: sucesso, sem warnings ou erros.
- `dotnet test`: **17 tests passaram**, nenhum ignorado ou falhando.
- `flutter analyze`: **sem issues**.
- `flutter test`: **18 tests passaram**.
- API reiniciada: OpenAPI `200`, com POST de OS e GET de Clientes publicados.
- Frontend recompilado e iniciado no Linux com a API base URL configurada.
- MySQL **8.4.13**, banco `desanti_dev`, tabelas `Clientes`, `OrdensServico` e
  `PecasOrdemServico`, todas **InnoDB**, verificados por conexão local.
- Cadastro fictício via API e criação de OS retornando **201**: peça com 3 × 101
  centavos e mão de obra de 12000 resultaram em **12303 centavos** persistidos.
- Alterar o nome do cliente manteve o nome histórico da OS.
- Rollback real: um nome de peça de 201 caracteres provocou erro MySQL 1406.
  Um `DbCommandInterceptor` observou a inserção anterior da OS; após a falha,
  uma nova consulta confirmou que OS/peça parciais não permaneceram no banco.
- Os dados fictícios criados pelo check foram removidos no `finally`, limitando
  a limpeza aos registros associados ao ID do cliente recém-criado.

### Repetir o check real, quando autorizado

Com API e MySQL locais ativos:

```bash
./scripts/verify-dev-database.sh
```

O wrapper carrega `.env.local` e executa `scripts/verify-dev-database.cs` como
file-based app do .NET 10. O código recusa qualquer destino diferente de
`127.0.0.1:3306`, database `desanti_dev` e usuário `desanti_app`. Ele inspeciona o
schema, usa a API para criar dados sintéticos, consulta a persistência com EF
Core, verifica o rollback real e remove seus registros. Não altera schema nem
executa migrations. Esse check **não** faz parte de `dotnet test` e escreve
dados temporários; não executá-lo contra outro ambiente ou sem autorização.

O interceptor do check apenas observa comandos executados; a falha é produzida
pelo limite real de `varchar(200)` no banco. A inserção é observada no SQL porque
o EF Core pode restaurar o ID da Entity para zero quando `SaveChangesAsync` falha.

## Limites e próximas etapas

- Manter o schema local em sincronia por uma estratégia de evolução acordada.
  O bootstrap de volume vazio não substitui migrations para bancos já existentes.
- Cadastro/edição de clientes na tela, listagem/detalhe de OS, finalização,
  idempotency e recuperação de rascunho são evoluções futuras.
- A definição fabricação/ano modelo da Falcon e o hosting de produção continuam
  pendentes. Esta integração não chama fornecedor externo.
- Não foram implementados authentication/authorization, deployment, migrations,
  commit ou push nesta etapa.
