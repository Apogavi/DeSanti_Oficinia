# DeSanti Automotiva — Status Consolidado e Handoff (10/11/2026)

Registro completo das entregas concluídas em **10/11/2026** (`[Antigravity / Gemini 4 Argon]`) para continuidade imediata por qualquer agente (`Claude`, `Codex` ou `Antigravity`).

---

## 1. Estrutura Atual do Repositório

- **[`backend/`](./backend/) (`C# ASP.NET Core / .NET 10 / EF Core 10.0.12 / MySQL 8.4.13`):**
  - Arquitetura em 4 camadas (`DeSanti.Domain`, `DeSanti.Application`, `DeSanti.Infrastructure`, `DeSanti.Api`).
  - CORS configurado em [`Program.cs`](./backend/DeSanti.Api/Program.cs) liberando `http://localhost:4200` e `http://127.0.0.1:4200`.
  - **46/46 testes automatizados passando** (`dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj`).
- **[`frontend/`](./frontend/) (`Angular 21 + TypeScript` — Ativo):**
  - Novo frontend web oficial aplicando [`design-studio`](/home/diosec/.gemini/config/skills/design-studio/SKILL.md) e [`apple-design`](/home/diosec/.gemini/config/skills/apple-design/SKILL.md) (**Apple HIG + Liquid Glass** com refração óptica real via SVG `feDisplacementMap`).
  - Arquivos principais:
    - [`src/index.html`](./frontend/src/index.html) e [`src/styles.css`](./frontend/src/styles.css) (tokens claros/escuros + `.liquid-glass`, `.liquid-glass-backdrop` e `.liquid-glass--modal`).
    - [`src/app/shared/liquid-glass.directive.ts`](./frontend/src/app/shared/liquid-glass.directive.ts) (diretiva standalone `[appLiquidGlass]` oficial da skill `apple-design`).
    - [`src/app/core/models.ts`](./frontend/src/app/core/models.ts), [`src/app/core/formatters.ts`](./frontend/src/app/core/formatters.ts) e [`src/app/core/api.service.ts`](./frontend/src/app/core/api.service.ts).
    - [`src/app/app.ts`](./frontend/src/app/app.ts), [`src/app/app.html`](./frontend/src/app/app.html) e [`src/app/app.css`](./frontend/src/app/app.css).
- **[`frontend_flutter_off/`](./frontend_flutter_off/) (`Flutter Desktop` — Desativado/Backup):**
  - Versão anterior em Flutter preservada por Diogo como backup (`30/30` testes passando).

---

## 2. O Que Foi Concluído Hoje (10/11/2026)

1. **Regra de Custo Obrigatório das Peças (`custoUnitarioCentavos >= 0`):**
   - **Backend:** [`CriarOrdemServicoValidator.cs`](./backend/DeSanti.Application/Validators/OrdensServico/CriarOrdemServicoValidator.cs) e [`OrdemServico.cs`](./backend/DeSanti.Domain/OrdemServico.cs) rejeitam peças com `custoUnitarioCentavos` nulo, omitido ou negativo (`400 Bad Request`).
   - **Frontend:** Modal de adicionar peça exige `Custo da peça (R$) *` e `Valor cobrado do cliente (R$) *` em centavos inteiros (`BigInt`/`number` seguro).
2. **Cadastro e Consulta de Clientes (CPF de 11 dígitos e CNPJ de 14 dígitos):**
   - **Backend:** [`CriarClienteValidator.cs`](./backend/DeSanti.Application/Validators/Clientes/CriarClienteValidator.cs), [`ClienteService.cs`](./backend/DeSanti.Application/ClienteService.cs) e [`ClienteController.cs`](./backend/DeSanti.Api/Controllers/ClienteController.cs) validam `nome`, `documento` (11 ou 14 dígitos numéricos) e `telefone` (10 ou 11 dígitos), normalizam apenas dígitos no banco, aceitam `email: ""` sem exigir migration e retornam `201 Created`.
   - **Frontend Angular:**
     - **Menu Suspenso no Topo em Liquid Glass:** Inicia exibindo apenas o botão sanduíche (3 traços) e, ao clicar, expande na horizontal exibindo `Ordens de serviço`, `Clientes` e alternância de tema claro/escuro.
     - **Aba dedicada `Clientes`:** Possui **Cadastrar cliente** (na esquerda, com ordem nativa de `Tab` perfeita) e **Consulta de clientes** (na direita, com filtro em tempo real por Nome/CPF/CNPJ e botão `Abrir OS` que já inicia uma OS com o cliente selecionado).
     - **Busca inteligente na `Nova Ordem de Serviço`:** Permite buscar cliente por Nome ou CPF/CNPJ; se não encontrar, exibe alerta imediato com o botão `+ Cadastrar cliente antes de prosseguir` (que já abre o modal Liquid Glass pré-preenchido com o nome ou CPF/CNPJ digitado e seleciona o cliente recém-criado).

---

## 3. Como Executar o Ambiente

```bash
# 1. Subir a API Backend (.NET 10 na porta 5085)
./scripts/dev-api.sh

# 2. Subir o Frontend Angular (porta 4200)
cd frontend
mise exec node@26.8.1 -- npm start
# (ou, se o node com @angular/cli estiver ativo no terminal: npm start)
```

---

## 4. Próximos Passos Pendentes do Roadmap (Fase 1 e Fase 2)

1. **Validação de persistência real E2E no MySQL (`desanti_dev`):** Cadastrar um cliente e uma OS com peça pela interface Angular (`http://localhost:4200`) e conferir os registros no DBeaver (ou rodar `./scripts/verify-dev-database.sh`).
2. **Consulta e Listagem de Ordens de Serviço (`GET /api/v1/ordens-servico`):** Criar endpoint de listagem/detalhes de OS no backend e a tabela/lista de consulta na aba `Ordens de serviço` do Angular (filtro por placa, cliente e status).
3. **Finalização de Ordem de Serviço:** Endpoint `POST` ou `PATCH` para registrar `DataConclusaoUtc` e calcular automaticamente `DataPrevistaRetornoUtc = DataConclusaoUtc + PrazoRetornoDias`.
4. **Fase 2 — Relatórios / Dashboard Financeiro:** Definir regras de período (semanal/mensal) e agregação mantendo discriminados o **líquido das peças** (`valorCobrado - custoFornecedor`) e a **mão de obra** integral.
