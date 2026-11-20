# DeSanti Automotiva — Roadmap Completo de Fases e Repasse para o Codex (10/11/2026)

Documento de sincronização e handoff técnico gerado em **10/11/2026** por **`[Antigravity / Gemini 4 Argon]`** para continuidade direta pelo **Codex**.

---

## 1. Resumo Executivo do que Foi Feito e Como Foi Construído (10/11/2026)

### 1.1. Skills Utilizadas e Onde Encontrá-las no Codex
Todo o trabalho visual e arquitetural do frontend seguiu estritamente as skills instaladas na máquina de Diogo:
- **No Codex (`/home/diosec/.agents/skills/`):**
  - **`design-studio`:** `/home/diosec/.agents/skills/design-studio/SKILL.md`
  - **`apple-design` (HIG + Liquid Glass + SVG Refraction):** `/home/diosec/.agents/skills/apple-design/SKILL.md`
    - Folha de estilo base: `/home/diosec/.agents/skills/apple-design/assets/liquid-glass.css`
    - Diretiva Angular oficial: `/home/diosec/.agents/skills/apple-design/assets/liquid-glass.directive.ts`
    - Receitas e armadilhas: `/home/diosec/.agents/skills/apple-design/references/recipes.md`
  - **`google-design` (Material 3):** `/home/diosec/.agents/skills/google-design/SKILL.md`
- **No Antigravity (`/home/diosec/.gemini/config/skills/`):** mesmos pacotes espelhados.

> **Diretriz para o Codex:** Antes de alterar ou criar qualquer componente no `frontend/`, leia `/home/diosec/.agents/skills/design-studio/SKILL.md` e `/home/diosec/.agents/skills/apple-design/SKILL.md`. Mantenha a estética **Apple HIG + Liquid Glass** sem clichês visuais de IA.

---

### 1.2. Migração do Frontend para Angular 21 + TypeScript (`frontend/`)
- A versão anterior em Flutter Desktop foi preservada intacta em [`frontend_flutter_off/`](./frontend_flutter_off/) (`30/30` testes passando).
- O novo frontend principal da aplicação agora está em **[`frontend/`](./frontend/)** usando **Angular 21 (Standalone Components + Signals + FormsModule) + TypeScript**:
  - **[`frontend/src/index.html`](./frontend/src/index.html):** Boot script de tema (`data-theme="light" | "dark"` persistido em `localStorage['desanti-theme']`) e classe `liquid-glass-backdrop` no `<body>` (Regra 1 do Liquid Glass: malha ambiental de luz para refração).
  - **[`frontend/src/styles.css`](./frontend/src/styles.css):** Tokens semânticos claros e escuros, classes oficiais `.liquid-glass`, `.liquid-glass-chip`, `.liquid-glass--modal`, `.liquid-glass-scrim`, `tabular-nums` e `-webkit-tap-highlight-color: transparent` (sem quadrado azul em dispositivos móveis).
  - **[`frontend/src/app/shared/liquid-glass.directive.ts`](./frontend/src/app/shared/liquid-glass.directive.ts):** Diretiva standalone `[appLiquidGlass]` portada 1:1 da skill `apple-design`, gerando o mapa de deslocamento em `<canvas>` e aplicando `backdrop-filter: url(#liquid-glass-...)` (`feImage` → `feDisplacementMap` → `feGaussianBlur`) fora da `NgZone`.
  - **[`frontend/src/app/core/models.ts`](./frontend/src/app/core/models.ts):** Contratos TypeScript para `Cliente`, `CriarClienteRequest`, `PecaItem`, `CriarOrdemServicoRequest`, `CriarOrdemServicoResponse` e `ApiException`.
  - **[`frontend/src/app/core/formatters.ts`](./frontend/src/app/core/formatters.ts):**
    - `formatarCpfOuCnpj`: máscara dinâmica que alterna automaticamente entre **CPF (11 dígitos: `###.###.###-##`)** e **CNPJ (14 dígitos: `##.###.###/####-##`)**.
    - `formatarTelefone`: máscara dinâmica para **10 dígitos (`(##) ####-####`)** ou **11 dígitos (`(##) #####-####`)**.
    - `formatarPlacaInput` / `normalizarPlaca`: máscara `AAA-0A00` e normalização sem hífen (`AAA0A00`).
    - `textoParaCentavos` / `formatarCentavos` / `formatarMoedaInput`: aritmética monetária estritamente em **centavos inteiros** (`number` seguro e `BigInt`, nunca `float` para cálculo financeiro).
  - **[`frontend/src/app/core/api.service.ts`](./frontend/src/app/core/api.service.ts):** Integração HTTP com `http://localhost:5085`, tratando `200`, `201`, `400` (`ValidationProblemDetails` por campo), `404`, `409` e bloqueio de reenvio em caso de `resultadoIncerto`.
  - **[`frontend/src/app/app.ts`](./frontend/src/app/app.ts), [`app.html`](./frontend/src/app/app.html) e [`app.css`](./frontend/src/app/app.css):**
    - **Menu Suspenso no Topo (`topbar-pill` com `appLiquidGlass`):** Inicia recolhido exibindo apenas o botão sanduíche (3 traços) e, ao clicar, expande na horizontal com curva elástica (`cubic-bezier(0.32, 0.72, 0, 1)`) revelando `Ordens de serviço`, `Clientes` e alternância de tema (`Modo claro` / `Modo escuro`).
    - **Aba `Clientes`:** Layout dividido em **Cadastrar cliente** (esquerda, com ordem nativa de `Tab`) e **Consulta de clientes** (direita, com filtro por Nome/CPF/CNPJ e botão `Abrir OS` que inicia uma OS já com o cliente selecionado).
    - **Tela `Nova Ordem de Serviço`:** Busca de cliente por Nome ou CPF/CNPJ (seleciona automaticamente se digitar os 11 ou 14 dígitos exatos; se não encontrar, exibe alerta amarelo com botão `+ Cadastrar cliente antes de prosseguir` que abre modal Liquid Glass já pré-preenchido com o termo digitado), dados do veículo, peças com custo obrigatório e resumo financeiro.

---

### 1.3. Entregas Concluídas no Backend (`backend/` — .NET 10)
- **Regra de Custo Obrigatório das Peças (`custoUnitarioCentavos >= 0`):**
  - [`PecaOrdemServico.cs`](./backend/DeSanti.Domain/PecaOrdemServico.cs), [`OrdemServico.cs`](./backend/DeSanti.Domain/OrdemServico.cs), [`CriarOrdemServicoRequest.cs`](./backend/DeSanti.Application/DTOs/OrdensServico/CriarOrdemServicoRequest.cs) e [`CriarOrdemServicoValidator.cs`](./backend/DeSanti.Application/Validators/OrdensServico/CriarOrdemServicoValidator.cs) exigem `custoUnitarioCentavos` não nulo e `>= 0` em toda peça de nova OS (`400 Bad Request` em `pecas[i].custoUnitarioCentavos` quando ausente, `null` ou negativo).
- **Validação e Normalização de Clientes (CPF/CNPJ + Telefone):**
  - [`CriarClienteValidator.cs`](./backend/DeSanti.Application/Validators/Clientes/CriarClienteValidator.cs), [`CreateClienteDto.cs`](./backend/DeSanti.Application/DTOs/Cliente/CreateClienteDto.cs), [`ClienteService.cs`](./backend/DeSanti.Application/ClienteService.cs) e [`ClienteController.cs`](./backend/DeSanti.Api/Controllers/ClienteController.cs) validam `nome` obrigatório (até 200 caracteres), `documento` (11 dígitos para CPF ou 14 dígitos para CNPJ, salvando apenas dígitos) e `telefone` (10 ou 11 dígitos, salvando apenas dígitos), aceitando `email: ""` para compatibilidade com a coluna `NOT NULL` do MySQL sem migration e retornando `201 Created`.
- **CORS Habilitado:**
  - [`Program.cs`](./backend/DeSanti.Api/Program.cs) libera `http://localhost:4200` e `http://127.0.0.1:4200`.
- **Suíte de Testes Backend:** **46/46 testes passando** (`dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj`).

---

## 2. Roadmap Detalhado das Próximas Fases (O Que Falta Fazer)

### Fase 1 — Fechamento da Operação Básica da Oficina (Prioridade Imediata)

#### Item 1.1 — Consulta e Listagem de Ordens de Serviço (Próxima Tarefa Recomendada)
- **Contexto:** Na aba `Ordens de serviço` do Angular, hoje existe o cabeçalho com o botão `+ Nova ordem de serviço` e o alerta da última OS criada, mas ainda não há a listagem das OS existentes no banco.
- **O que construir no Backend (`backend/`):**
  1. DTOs de leitura em `DeSanti.Application/DTOs/OrdensServico/`:
     - `ResumoOrdemServicoResponse` (`Id`, `ClienteId`, `NomeCliente`, `DocumentoCliente`, `TelefoneCliente`, `Placa`, `Marca`, `Modelo`, `Cor`, `Ano`, `MaoDeObraCentavos`, `TotalCentavos`, `NecessitaRetorno`, `PrazoRetornoDias`, `DataCriacaoUtc`, `DataConclusaoUtc`, `DataPrevistaRetornoUtc`).
     - `DetalheOrdemServicoResponse` (campos do resumo + `DescricaoServico` + lista de peças com `Id`, `Nome`, `Quantidade`, `ValorUnitarioCentavos`, `CustoUnitarioCentavos`).
  2. Métodos em `IOrdemServicoRepository` / `OrdemServicoRepository` (`AsNoTracking()`, ordenados por `DataCriacaoUtc DESC`) e `IOrdemServicoService` / `OrdemServicoService`.
  3. Endpoints em [`OrdemServicoController.cs`](./backend/DeSanti.Api/Controllers/OrdemServicoController.cs):
     - `GET /api/v1/ordens-servico`
     - `GET /api/v1/ordens-servico/{id:long}` (`200 OK` ou `404 NotFound`).
  4. Testes em `DeSanti.Api.Tests`.
- **O que construir no Frontend (`frontend/`):**
  1. Métodos `listarOrdensServico()` e `obterOrdemServicoPorId(id)` em [`api.service.ts`](./frontend/src/app/core/api.service.ts).
  2. Na tela inicial da aba `Ordens de serviço` ([`app.html`](./frontend/src/app/app.html)):
     - Barra de busca rápida por **Placa**, **Nome do cliente** ou **CPF/CNPJ** e filtro rápido de status (`Todas`, `Em aberto`, `Concluídas`).
     - Lista/cards das OS mostrando `#ID`, Placa formatada, Veículo (`Marca Modelo Cor`), Cliente, Data de abertura, Status (`Em aberto` / `Concluída`) e `Total (R$)` em `tabular-nums`.
     - Modal `.liquid-glass--modal` (ou painel expansível) para visualizar os detalhes completos da OS e suas peças.

#### Item 1.2 — Finalização de Ordem de Serviço e Previsão de Retorno
- **Regra de Negócio Confirmada (`AGENTS.md`):**
  - Na abertura da OS, `DataConclusaoUtc` e `DataPrevistaRetornoUtc` ficam `null`.
  - Ao finalizar a OS, registra-se `DataConclusaoUtc = DateTime.UtcNow` e, se `NecessitaRetorno == true` e `PrazoRetornoDias > 0`, calcula-se `DataPrevistaRetornoUtc = DataConclusaoUtc.Value.AddDays(PrazoRetornoDias.Value)`.
- **O que construir no Backend (`backend/`):**
  1. Método de domínio `OrdemServico.Finalizar(DateTime dataConclusaoUtc)` em [`OrdemServico.cs`](./backend/DeSanti.Domain/OrdemServico.cs) (rejeitando se já estiver concluída).
  2. Endpoint `POST /api/v1/ordens-servico/{id:long}/finalizar` retornando `200 OK` (com `id`, `dataConclusaoUtc`, `dataPrevistaRetornoUtc`), `404 NotFound` se não existir ou `409 Conflict` se já estiver finalizada.
  3. Testes de unidade em `OrdemServicoTests.cs` e de integração em `OrdemServicoApiTests.cs`.
- **O que construir no Frontend (`frontend/`):**
  1. Botão `Finalizar OS` nas OS em aberto na listagem/detalhes, atualizando imediatamente o status para `Concluída` e exibindo a data prevista de retorno formatada.

#### Item 1.3 — Edição Rápida de Cliente na Aba Clientes (Opcional Fase 1)
- Ligar o `PUT /api/v1/clientes` existente para permitir editar Nome, CPF/CNPJ ou Telefone de um cliente cadastrado na aba `Clientes`.

---

### Fase 2 — Relatórios e Resumo Financeiro (Semanal e Mensal)

- **Regras Financeiras Confirmadas por Diogo (10/11/2026):**
  - **Bruto das peças:** soma do valor cobrado do cliente (`quantidade * valorUnitarioCentavos`).
  - **Custo das peças:** soma do custo pago ao fornecedor (`quantidade * custoUnitarioCentavos`).
  - **Líquido das peças:** `bruto das peças - custo das peças`.
    - *Exemplo 1:* Bomba d'água comprada por `R$ 50,00` e cobrada por `R$ 120,00` gera `R$ 70,00` líquidos na peça.
    - *Exemplo 2:* Bico comprado por `R$ 30,00` e cobrado por `R$ 70,00` gera `R$ 40,00` líquidos na peça.
  - **Mão de obra:** valor separado que não possui custo de fornecedor de peça e entra integralmente no líquido operacional da OS.
  - **Líquido operacional da OS:** `líquido das peças + mão de obra`. (Não desconta custos fixos da oficina e não deve ser rotulado como lucro líquido total do negócio).
  - **Discriminação obrigatória:** Mesmo quando somados em um total geral, manter **mão de obra** e **resultado das peças** sempre discriminados separadamente na tela.
- **Pendências a alinhar com Diogo antes de implementar a Fase 2:**
  - Critério de data do filtro semanal/mensal (por `DataCriacaoUtc` ou por `DataConclusaoUtc` / apenas OS concluídas vs. todas as OS).
  - Adicionar a 3ª opção `Financeiro / Relatórios` no menu suspenso Liquid Glass.

---

### Fase 3 — Geração de PDF da OS e Compartilhamento por WhatsApp

- Gerar visualização de impressão/PDF da OS para o cliente (exibindo apenas os valores cobrados do cliente e ocultando o `custoUnitarioCentavos` interno da oficina).
- Botão direto para compartilhar resumo/PDF no WhatsApp do cliente (`https://wa.me/55{telefone}`).

---

### Fase 4 (Futuro / Talvez) — Consulta de Veículo pela Placa (Falcon Data Hub)

- Consultar fornecedor Falcon pelo backend após digitação de placa válida (`7` caracteres normalizados), sugerindo `Marca`, `Modelo`, `Cor` e `Ano` sem sobrescrever edições manuais posteriores. Token Falcon restrito ao backend (nunca versionado).

---

## 3. Comandos Rápidos de Verificação e Execução

```bash
# Backend (.NET 10) — Build e 46 testes automatizados
cd /home/diosec/Projects/DeSanti_Automotiva
dotnet build backend/DeSanti.Api/DeSanti.API.csproj
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj

# Iniciar API local (porta 5085)
./scripts/dev-api.sh

# Frontend (Angular 21 + TS) — Build e Dev Server (porta 4200)
cd /home/diosec/Projects/DeSanti_Automotiva/frontend
mise exec node@26.8.1 -- npm run build
mise exec node@26.8.1 -- npm start
```
