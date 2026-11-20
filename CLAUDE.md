# DeSanti Automotiva — Diretrizes do Projeto (Claude)

Guia de contexto e diretrizes operacionais para o Claude no repositório **DeSanti Automotiva**.

---

## 1. Fontes Canônicas e Limites de Atuação

- **Fonte Canônica de Negócio:** [`AGENTS.md`](./AGENTS.md) é a autoridade máxima compartilhada sobre papéis, regras da oficina, limites de atuação, histórico de decisões e roadmap.
- **Diretrizes Complementares Antigravity/Gemini:** [`GEMINI.md`](./GEMINI.md) contém o mapa detalhado de ferramentas, skills e MCPs daquele ecossistema.
- **Regras de Atuação:**
  - O modo padrão de colaboração é orientar, revisar e explicar tanto o backend quanto o frontend (Diogo desenvolve manualmente no VS Code).
  - **Não edite código, project files, dependencies, schema de banco ou Git sem solicitação explícita de Diogo** para a etapa correspondente.
  - Nunca execute migrations, alterações destrutivas de schema, deploy, `git commit` ou `git push` por conta própria.
  - Preserve as alterações locais e a working tree de Diogo e Thalles.

---

## 2. Visão Geral da Arquitetura e Stack Técnica

O projeto é um ERP para a oficina mecânica DeSanti Automotiva, dividido em dois pilares:

### Backend (`backend/`) — .NET 10 / ASP.NET Core / EF Core 10.0.12 / MySQL
Monolith em Clean Architecture com 4 camadas:
- **`DeSanti.Domain`:** Entidades puras (`Cliente`, `OrdemServico`, `PecaOrdemServico`), invariants e cálculos de domínio. Sem dependência de EF Core ou frameworks.
- **`DeSanti.Application`:** Use cases, Application Services (`ClienteService`, `OrdemServicoService`), DTOs, validadores FluentValidation e interfaces de Repositories.
- **`DeSanti.Infrastructure`:** Persistência via EF Core (`MySql.EntityFrameworkCore` 10.0.12), `AppDbContext`, mappings relacionais e repositórios concretos (`OrdemServicoRepository` com transação explícita para OS + peças).
- **`DeSanti.Api`:** Controllers REST (`ClienteController`, `OrdemServicoController`), middleware de erros, DI composition root.
- **`DeSanti.Api.Tests`:** Testes de unidade e de integração com `Microsoft.AspNetCore.Mvc.Testing`.

### Frontend (`frontend/`) — Angular 21 + TypeScript (Ativo)
> Nota: A versão anterior em Flutter foi movida para `frontend_flutter_off/`.
- **Design System (`design-studio` + `apple-design`):** Apple HIG + **Liquid Glass** nativo (`src/styles.css` + diretiva `src/app/shared/liquid-glass.directive.ts` com refração óptica via SVG `feDisplacementMap`).
- **Menu Suspenso no Topo:** Cápsula Liquid Glass com botão sanduíche (3 traços) que expande na horizontal (`Ordens de serviço` | `Clientes` | Tema).
- **Aba Clientes:** Cadastro (`Nome`, `CPF/CNPJ`, `Telefone`) e Consulta integrados lado a lado, com `Tab` nativo do HTML.
- **Nova Ordem de Serviço:** Busca de cliente por Nome ou CPF/CNPJ (com alerta automático para cadastrar quando não encontrado), veículo, peças com custo obrigatório em centavos inteiros e resumo financeiro.

---

## 3. Comandos Úteis e Fluxo de Execução

### Backend (.NET)
```bash
# Compilar a API
dotnet build backend/DeSanti.Api/DeSanti.API.csproj

# Rodar a suíte completa de testes
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj

# Iniciar a API em modo desenvolvimento (porta 5085)
./scripts/dev-api.sh
```

### Frontend (Flutter)
```bash
cd frontend

# Análise estática (deve estar com 0 issues)
flutter analyze --no-pub

# Testes de widget e unidade (26 testes passando)
flutter test --no-pub

# Executar a aplicação desktop no Linux
flutter run -d linux
```

### Banco de Dados Local (Docker MySQL 8.4.13)
- Host: `127.0.0.1:3306` | Database: `desanti_dev` | User: `desanti_app`
- Credenciais reais em `.env.local` (ignorado pelo Git).
- No DBeaver: ativar `allowPublicKeyRetrieval=true` e `useSSL=false`.

---

## 4. Regras Financeiras e Contábeis Consolidadas (10/11/2026)

Conforme alinhamento confirmado por Diogo em [`AGENTS.md`](./AGENTS.md) e [`backend/docs/CUSTO_PECAS.md`](./backend/docs/CUSTO_PECAS.md):

1. **Dinheiro em Centavos Inteiros:** Sempre usar inteiros (`long` em C#, `int` ou `BigInt` em Dart) para centavos. Proibido usar `float` ou `double` para valores monetários. Exemplo: `12000` = `R$ 120,00`.
2. **Líquido da Peça:** `valor cobrado do cliente - custo pago ao fornecedor`. Exemplo: peça comprada por `R$ 50` e cobrada por `R$ 120` gera `R$ 70` líquidos na peça.
3. **Mão de Obra:** Entra integralmente no líquido operacional calculado para a OS (não possui custo de fornecedor nem campo separado de bruto).
4. **Líquido Operacional da OS:** `soma do líquido de todas as peças + valor integral da mão de obra`. Esse valor **não** desconta custos fixos da oficina e **não** deve ser rotulado como lucro líquido total.
5. **Obrigatoriedade do Custo da Peça:** Toda peça deve ter custo conhecido obrigatoriamente (`custoUnitarioCentavos >= 0`). Backend (`CriarOrdemServicoValidator`, `OrdemServico.Criar`) e frontend (`pecas_section.dart`, `PecaItem`, `CriarPecaRequest`) já aplicam essa regra na criação de novas OS (`32` tests no backend e `27` tests no frontend passando). A coluna no banco permanece `bigint NULL` apenas por compatibilidade histórica.
6. **Discriminação Obrigatória:** Em relatórios ou dashboards, manter mão de obra e peças sempre discriminados separadamente, mesmo quando somados no líquido total.

---

## 5. Restrição Crítica de Hardware (PC da Oficina)

O computador alvo na oficina é **modesto/fraco**:
- **Design Studio / UI:** Superfícies tonais sólidas, bordas limpas de `1px`, tipografia com alto contraste e `const` widgets.
- **Zero GPU Blur:** **Não** utilizar `BackdropFilter`, `ImageFilter.blur` em tempo real, sombras difusas pesadas ou animações contínuas.
- **Produção:** Compilar sempre em `--release` para Windows. No MySQL local em máquina fraca, limitar `innodb_buffer_pool_size` para não esgotar a RAM.

---

## 6. Estado Atual e Próximos Passos (Fase 1)

1. **[Concluído em 10/11/2026] Adequação do custo obrigatório de peças:** Backend (`CriarOrdemServicoValidator`, `OrdemServico.Criar`) e frontend (`pecas_section.dart`, `PecaItem`, `CriarPecaRequest`) exigem custo conhecido (`>= 0`) em novas peças.
2. **[Concluído em 10/11/2026] Cadastro de Clientes (CPF/CNPJ + Telefone):** Backend (`CriarClienteValidator`, `ClienteController`, `ClienteService` retornando `201 Created`) e frontend (`novo_cliente_dialog.dart`, botão **Novo cliente** na `InicioPage` e busca por Nome ou CPF/CNPJ na `NovaOrdemPage` com alerta para cadastrar quando não encontrado). `46` tests no backend e `30` tests no frontend passando.
3. **Validação de persistência real no MySQL:** Executar `POST /api/v1/ordens-servico` com a API ativa e validar no DBeaver se os centavos foram persistidos.
4. **Commit do trabalho no Git:** Organizar em commits atômicos as implementações de integração da OS, suporte a custos, cadastro de clientes e redesign visual.
5. **Consulta e Listagem de OS:** Endpoints e telas de consulta/listagem com busca leve por placa e cliente.
6. **Finalização de OS:** Endpoint para registrar conclusão e calcular data prevista de retorno.
