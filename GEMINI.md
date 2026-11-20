# DeSanti Automotiva — Diretrizes Antigravity / Gemini / Codex

Atualizado em **10/11/2026** (`[Antigravity / Gemini 4 Argon]`).

## 1. Relação com o `AGENTS.md` e Documentos de Repasse

- **[`AGENTS.md`](/home/diosec/Projects/DeSanti_Automotiva/AGENTS.md) é a fonte canônica compartilhada (`Codex <-> Antigravity`)** para papel do agente, limites de atuação, regras de negócio da oficina, API contracts, Clean Architecture no backend e histórico de decisões.
- **[`ROADMAP_E_REPASSE_CODEX.md`](/home/diosec/Projects/DeSanti_Automotiva/ROADMAP_E_REPASSE_CODEX.md)** e **[`STATUS_PROJETO_10_11_2026.md`](/home/diosec/Projects/DeSanti_Automotiva/STATUS_PROJETO_10_11_2026.md)** contêm o detalhamento completo do que foi construído em **10/11/2026**, como foi implementado, as skills usadas (`design-studio` e `apple-design`) e o passo a passo de cada fase pendente (Fases 1, 2, 3 e 4).
- Todas as restrições de [`AGENTS.md`](/home/diosec/Projects/DeSanti_Automotiva/AGENTS.md) têm precedência absoluta: **não editar código, project files, dependencies, banco/migrations ou Git sem solicitação explícita de Diogo para a etapa correspondente**.

## 2. Protocolo de Sincronização (`Codex <-> Antigravity`)

- Antes de orientar ou implementar qualquer mudança, leia [`AGENTS.md`](/home/diosec/Projects/DeSanti_Automotiva/AGENTS.md), [`ROADMAP_E_REPASSE_CODEX.md`](/home/diosec/Projects/DeSanti_Automotiva/ROADMAP_E_REPASSE_CODEX.md), [`INTEGRACAO_OS.md`](/home/diosec/Projects/DeSanti_Automotiva/backend/docs/INTEGRACAO_OS.md) e [`CUSTO_PECAS.md`](/home/diosec/Projects/DeSanti_Automotiva/backend/docs/CUSTO_PECAS.md).
- Ao atualizar [`AGENTS.md`](/home/diosec/Projects/DeSanti_Automotiva/AGENTS.md) ou documentos compartilhados, preserve os registros existentes e identifique a intervenção com a tag do agente/modelo ativo e a data.

## 3. Regras Financeiras e Contábeis Consolidadas (10/11/2026)

- **Líquido da peça:** `valor cobrado do cliente - custo pago ao fornecedor`. Exemplo: peça comprada por `R\$ 50` e cobrada por `R\$ 120` gera `R\$ 70` de líquido na peça.
- **Mão de obra:** entra integralmente no líquido operacional da OS (não possui custo de fornecedor nem campo separado de bruto).
- **Líquido operacional da OS:** `soma do líquido de todas as peças + valor integral da mão de obra`. Esse montante não desconta custos fixos/despesas da oficina e **não deve ser apresentado como lucro líquido total do negócio**.
- **Obrigatoriedade do custo da peça (`Concluído em 10/11/2026`):** Toda peça deve ter custo conhecido (`custoUnitarioCentavos >= 0`). Backend (`CriarOrdemServicoValidator`, `OrdemServico.Criar`) e Frontend (`Angular + TS` em `frontend/` e backup Flutter em `frontend_flutter_off/`) já exigem custo conhecido em toda nova OS. A coluna `CustoUnitarioCentavos` no banco permanece `bigint NULL` por compatibilidade histórica.
- **Discriminação obrigatória:** Em qualquer relatório ou visualização futura de dashboard, manter mão de obra e resultado de peças discriminados individualmente, mesmo quando somados no líquido total.

## 4. Skills de Design e UI/UX (`design-studio` + `apple-design`)

Em **10/11/2026**, Diogo definiu que o frontend principal fosse migrado para **Angular 21 + TypeScript** em [`frontend/`](/home/diosec/Projects/DeSanti_Automotiva/frontend) (mantendo o antigo Flutter em [`frontend_flutter_off/`](/home/diosec/Projects/DeSanti_Automotiva/frontend_flutter_off)), aplicando a estética **Apple HIG + Liquid Glass** com menu suspenso no topo (botão sanduíche de 3 traços que expande na horizontal com curva elástica do iOS).

| Skill | Caminho no Antigravity | Caminho no Codex | Função no DeSanti Automotiva |
| :--- | :--- | :--- | :--- |
| **`design-studio`** | [`SKILL.md`](/home/diosec/.gemini/config/skills/design-studio/SKILL.md) | `/home/diosec/.agents/skills/design-studio/SKILL.md` | Orquestrador visual principal combinando Apple HIG + Liquid Glass e tokens semânticos M3. |
| **`apple-design`** | [`SKILL.md`](/home/diosec/.gemini/config/skills/apple-design/SKILL.md) | `/home/diosec/.agents/skills/apple-design/SKILL.md` | Fornece [`liquid-glass.css`](/home/diosec/.gemini/config/skills/apple-design/assets/liquid-glass.css) e a diretiva Angular [`liquid-glass.directive.ts`](/home/diosec/.gemini/config/skills/apple-design/assets/liquid-glass.directive.ts) (`[appLiquidGlass]`) usada no projeto. |
| **`google-design`** | [`SKILL.md`](/home/diosec/.gemini/config/skills/google-design/SKILL.md) | `/home/diosec/.agents/skills/google-design/SKILL.md` | Ergonomia de formulários, estados (`loading`, `error`, `empty`) e acessibilidade. |

## 5. Mapa Rápido da Estrutura Verificada (10/11/2026)

### Backend (`/home/diosec/Projects/DeSanti_Automotiva/backend` — .NET 10 / EF Core 10.0.12 / MySQL)
- **Solution:** [`DeSanti.slnx`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.slnx)
- **Domain (`DeSanti.Domain`):** [`Cliente.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Domain/Cliente.cs), [`OrdemServico.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Domain/OrdemServico.cs) e [`PecaOrdemServico.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Domain/PecaOrdemServico.cs).
- **Application (`DeSanti.Application`):**
  - Services: [`ClienteService.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Application/ClienteService.cs) e [`OrdemServicoService.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Application/OrdemServicoService.cs).
  - Validators: [`CriarClienteValidator.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Application/Validators/Clientes/CriarClienteValidator.cs) (CPF 11 dígitos / CNPJ 14 dígitos e telefone 10/11 dígitos) e [`CriarOrdemServicoValidator.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Application/Validators/OrdensServico/CriarOrdemServicoValidator.cs) (custo obrigatório `>= 0`).
- **Infrastructure (`DeSanti.Infrastructure`):** [`AppDbContext.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Infrastructure/Data/AppDbContext.cs), [`ClienteRepository.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Infrastructure/Repositories/ClienteRepository.cs) e [`OrdemServicoRepository.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Infrastructure/Repositories/OrdemServicoRepository.cs).
- **API (`DeSanti.Api`) e Tests (`DeSanti.Api.Tests`):**
  - [`Program.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Api/Program.cs) (CORS ativo para `localhost:4200`), [`ClienteController.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Api/Controllers/ClienteController.cs) (`201 Created` e `400 ValidationProblemDetails`), [`OrdemServicoController.cs`](/home/diosec/Projects/DeSanti_Automotiva/backend/DeSanti.Api/Controllers/OrdemServicoController.cs).
  - **46/46 testes passando** em `DeSanti.Api.Tests`.

### Frontend Principal (`/home/diosec/Projects/DeSanti_Automotiva/frontend` — Angular 21 + TypeScript)
- **Estilos e Liquid Glass:** [`index.html`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/index.html), [`styles.css`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/styles.css) e [`liquid-glass.directive.ts`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/shared/liquid-glass.directive.ts).
- **Core (Contratos, Máscaras e API):** [`models.ts`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/core/models.ts), [`formatters.ts`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/core/formatters.ts) e [`api.service.ts`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/core/api.service.ts).
- **Componentes e Views (`Ordens de serviço`, `Nova OS` e `Clientes`):** [`app.ts`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/app.ts), [`app.html`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/app.html) e [`app.css`](/home/diosec/Projects/DeSanti_Automotiva/frontend/src/app/app.css).
- **Comando de build/execução com `mise`:** `mise exec node@26.8.1 -- npm start` (porta `4200`).

## 6. Eficiência de Tokens e Contexto (19/11/2026)

- Para otimizar consumo de tokens e manter sessões enxutas, utilize sob demanda a skill global `coding-agent-token-efficiency` ([`SKILL.md`](/home/diosec/.gemini/config/skills/coding-agent-token-efficiency/SKILL.md)).
