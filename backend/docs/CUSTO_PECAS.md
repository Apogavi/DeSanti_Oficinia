# Custo das peças — backend e frontend, 04/11/2026 e 10/11/2026

## Regras confirmadas

- `valorUnitarioCentavos` continua sendo o preço unitário cobrado do cliente.
- `custoUnitarioCentavos` é o custo unitário da peça nesta OS, em centavos inteiros (`Int64`, obrigatório para novas criações desde **10/11/2026**).
- Na criação de OS (`POST /api/v1/ordens-servico`), campo omitido, `null` ou negativo retorna `400`, com a chave `pecas[i].custoUnitarioCentavos` em `errors`. Zero (`0`) representa custo conhecido de zero.
- O custo não altera `totalCentavos`: o cliente paga peças pelo preço cobrado, mais mão de obra.
- Peças históricas no banco permanecem com coluna `NULL` até definição de tratamento; na criação de novas OS toda peça deve ter custo conhecido.
- Cada OS armazena seu próprio custo. Não existe consulta de preço atual de fornecedor.

## API contract

O endpoint continua sendo `POST /api/v1/ordens-servico`. Exemplo de item:

```json
{
  "nome": "Peça teste",
  "quantidade": 3,
  "valorUnitarioCentavos": 101,
  "custoUnitarioCentavos": 70
}
```

O valor cobrado dessas peças é 303 centavos; seu custo é 210 centavos.
A resposta continua `201` com `id` e `totalCentavos`. Requests sem `custoUnitarioCentavos` ou com valor `null` retornam `400 Bad Request` desde 10/11/2026.
Não foi imposta relação entre custo e preço cobrado: são valores independentes.

## Arquivos envolvidos

- Domain: `PecaOrdemServico.CustoUnitarioCentavos`; `OrdemServico.Criar` rejeita custo nulo ou negativo.
- Application: `PecaRequest` (`long?` sem default no C# para preservar detecção de campo omitido no JSON), validation obrigatória por campo (`CriarOrdemServicoValidator`) e mapping no `OrdemServicoService`.
- Infrastructure: coluna `CustoUnitarioCentavos` no mapping EF Core de `PecasOrdemServico` (mantida `bigint NULL` no schema por compatibilidade histórica).
- Bootstrap: `schema_criacao_referencia.sql` atualizado somente para bancos novos.
- Exemplo manual: `DeSanti.Api/DeSanti.API.http`.

## Banco existente — atualização local executada em 04/11/2026

O arquivo `sql/2026-11-04_custo_pecas.proposta.sql` contém a alteração incremental
aplicada no MySQL local `127.0.0.1:3306`, banco `desanti_dev`, após autorização
explícita de Diogo. Não é uma EF migration e não há execução automática.
A autorização corresponde ao ambiente local; outros ambientes exigem revisão.

Antes de aplicar, confirmar ambiente, backup e ausência da coluna. O `ALTER TABLE`
do MySQL faz commit implícito; não tratar como uma transaction reversível.
Não reaplicar se a coluna já existir. Não remover o volume nem executar o bootstrap
num banco existente. Acrescentar a coluna opcional preserva os dados anteriores
com `NULL` e permite que a versão anterior da API continue funcionando.

Ordem de aplicação: atualizar o banco autorizado, depois reiniciar a API com o
novo código. Até a coluna existir, a nova API não está pronta para gravar OS no
banco antigo: o mapping passa a incluir a coluna mesmo quando o custo é nulo.

## Verificação

`dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj --no-restore`

Os tests cobrem custo nulo/zero/positivo, rejeição de negativo, preservação do
valor no Repository fake, request antigo sem o campo e coluna nullable no SQL
produzido pelo provider. Eles não comprovam persistência real da coluna.

Resultado observado em 04/11/2026: build dos projetos durante `dotnet test`
concluído e **27 tests passaram**, sem falhas ou ignorados. `git diff --check`
não apontou problemas de whitespace. Esses tests ocorreram antes da aplicação do SQL no banco.

## Resultado da atualização local

- Diogo autorizou aplicar a alteração em 04/11/2026.
- Destino conferido: `127.0.0.1:3306`, database `desanti_dev`, usuário `desanti_app`.
- Snapshot das peças antes do ALTER em arquivo local temporário com permissão `600`:
  `/tmp/desanti-schema-check/pecas-before-20261104144448.json`.
- `ALTER TABLE` executado; metadata confirmou `CustoUnitarioCentavos` como
  `bigint`, permitindo `NULL`.
- A tabela de peças estava vazia: zero registros antes e depois. A comparação
  dos campos anteriores não detectou mudanças. Nenhum registro sintético foi criado.
- Não foi realizado POST com custo nem verificação de persistência de um novo valor.

## Próximas etapas

1. Verificar a persistência do custo por POST no MySQL com dados fictícios,
   quando autorizado.
2. Flutter adaptado com “Custo da peça” e “Valor cobrado do cliente”,
   Model/DTO/ViewModel/JSON e 26 tests passando; análise sem issues.
   A coluna necessária agora existe no banco local.
3. Diogo definiu em 10/11/2026 o líquido da peça como valor cobrado do cliente
   menos custo do fornecedor: R\$ 120 cobrados menos R\$ 50 de custo resultam em
   R\$ 70 líquidos na peça. A mão de obra é separada e entra integralmente no
   líquido calculado da OS, somada ao líquido das peças, sem custo de fornecedor
   de peça a descontar. Não há campo separado de bruto para a mão de obra. O
   resultado não desconta outros custos e não é necessariamente o lucro líquido
   da oficina. Diogo definiu que toda peça deve ter custo conhecido.
4. Em **10/11/2026** (`[Antigravity / Gemini 4 Argon]`), Diogo autorizou e foi
   implementada a adequação do custo obrigatório na criação de OS:
   - **Backend:** `CriarOrdemServicoValidator` e `OrdemServico.Criar` exigem
     `custoUnitarioCentavos` conhecido (`>= 0`), rejeitando `null`, campo omitido
     ou valor negativo com `400 Bad Request` (`pecas[i].custoUnitarioCentavos`).
     `dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj`: **32 tests passaram**.
   - **Frontend:** `PecaItem`, `CriarPecaRequest` e `pecas_section.dart` tornaram
     `custoUnitarioCentavos` obrigatório no diálogo e nos models. `flutter analyze --no-pub`
     sem issues e `flutter test --no-pub` com **27 tests passando**.
   - A coluna `CustoUnitarioCentavos` no banco permanece `bigint NULL` até definir
     eventual migração de registros históricos. Ainda é preciso definir período e
     agregação, inclusive quantidades, no dashboard.
