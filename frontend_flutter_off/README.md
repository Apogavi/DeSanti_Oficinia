# Frontend DeSanti Automotiva

Estado revisado em **29/10/2026**. Aplicativo Flutter para desenvolvimento no
Linux; o target Windows deve ser compilado e validado em uma máquina Windows.

## Fluxo implementado

A tela inicial abre o formulário de OS. Ele consulta `GET /api/v1/clientes`,
oferece os clientes ativos para seleção e mostra os dados do cliente escolhido.
O cadastro de clientes na tela fica para outra etapa.

O formulário mantém veículo, descrição, peças, mão de obra e retorno. Placa
recebe máscara; ano opcional aceita até quatro dígitos. Valores monetários são
inteiros em centavos, formatados durante a digitação (`12000` → `120,00`).
CPF/telefone passam a ser dados consultados do cadastro, em vez de novos campos
editáveis na OS. Os InputFormatters existentes de CPF/telefone foram preservados
para futuros formulários de Clientes.

O botão **Salvar ordem de serviço** valida os campos e envia
`POST /api/v1/ordens-servico` com `clienteId`. O backend é responsável pelo
snapshot do cliente, total oficial e datas. No sucesso, a tela inicial mostra o
ID e o total da resposta `201`. Ela ainda não consulta uma listagem de OS.

- Carregamento de clientes, envio, sucesso e falha têm estados explícitos.
- Falhas preservam o rascunho em memória. Timeout ou resposta incerta bloqueiam
  reenvio nessa página, evitando repetir automaticamente uma criação.
- Retorno exige prazo positivo; desmarcar envia prazo nulo. Não há previsão de
  retorno antes da finalização da OS, que ainda não foi implementada.
- Todos os campos permanecem montados por `SingleChildScrollView`; validation no
  topo continua funcionando com o botão no fim da tela.
- A prévia usa `BigInt` para detectar totais que excedam o `Int64` do backend,
  sem cálculos com `double`. A API recalcula os valores recebidos.

## Organização e documentação do código

A integração usa **MVVM com Repositories e Services**, `ChangeNotifier` e
`ListenableBuilder`, sem Riverpod. O `AppTheme` existente foi preservado.

| Caminho | Responsabilidade |
| --- | --- |
| `lib/main.dart` | Composition root, Client HTTP e navegação; confirmação de sucesso |
| `lib/core/config/app_config.dart` | API base URL por `--dart-define` |
| `lib/core/network/api_service.dart` | HTTP, JSON, timeout e erros tipados |
| `lib/features/clientes/` | Model de Cliente e Repository de consulta |
| `lib/features/ordens_servico/nova_ordem_page.dart` | Widgets, controllers e navegação |
| `lib/features/ordens_servico/view_models/nova_ordem_view_model.dart` | Rascunho, ações, validation, prévias e state management |
| `lib/features/ordens_servico/repositories/` | Adaptação do API contract de criação |
| `lib/features/ordens_servico/models/` | Models de peças e resposta da criação |
| `lib/shared/validation/` | Validation imediata compartilhada |
| `lib/shared/formatters/` | Input masks e formatação monetária |

O guia [INTEGRACAO_OS.md](../backend/docs/INTEGRACAO_OS.md) explica o fluxo completo,
as decisões, o API contract e os tests. A
[ARQUITETURA_FRONTEND.md](ARQUITETURA_FRONTEND.md) registra a análise histórica de
28/10/2026; suas descrições de ausência de ViewModel/HTTP correspondem àquele
snapshot e foram superadas pela implementação desta integração.

## Execução

Na raiz, prepare e inicie o banco conforme o [README do backend](../backend/README.md)
e execute `./scripts/dev-api.sh`. Em outro terminal:

```bash
cd frontend
flutter pub get
flutter run -d linux --dart-define=API_BASE_URL=http://localhost:5085
```

O valor padrão é `http://localhost:5085`. A URL é uma configuração pública;
credenciais do MySQL e tokens de fornecedores pertencem apenas ao backend.
Mudar o `--dart-define` exige reiniciar o app. Em outro dispositivo, `localhost`
indica o próprio dispositivo; acesso via LAN, Flutter Web/CORS e acesso remoto
não foram validados nesta etapa.

## Checks

```bash
flutter analyze --no-pub
flutter test --no-pub
```

A suíte cobre JSON/HTTP com `MockClient`, ViewModel com Repositories simulados,
validation ao rolar, preservação dos campos após erro, fluxo de sucesso e
layout estreito. Esses tests não consultam banco real. O resultado final dos
checks está no guia de integração.

## Limites atuais

Não há cadastro de cliente na tela, consulta/listagem de OS, finalização, PDF,
WhatsApp ou Falcon. Ainda não há idempotency key nem rascunho persistente: fechar
o app perde os dados não salvos, e abrir outra página não impede globalmente
criações duplicadas. Conferir o servidor antes de repetir uma operação incerta.

## Custo das peças — 04/11/2026

O diálogo **Adicionar peça** agora oferece **Custo da peça (R$)** opcional e
**Valor cobrado do cliente (R$)** obrigatório, ambos por unidade e com a máscara
monetária existente. Custo vazio vira `null`; preencher zero mantém zero.
O card da peça mostra o custo unitário ou “não informado”, além do preço cobrado.
O custo segue no Model, ViewModel e JSON como `custoUnitarioCentavos` e não altera
a prévia do total cobrado. Não há cálculo de lucro ou dashboard nesta etapa.

Verificação: `flutter analyze --no-pub` sem issues; `flutter test --no-pub` com
**26 tests passando**. Os novos tests cobrem o diálogo, apagar custo preenchido,
null/zero/valor positivo, rejeição de negativo no ViewModel e envio em centavos.
A suíte existente também cobre janela estreita e preservação do rascunho.

A consulta inicial ao MySQL local em 04/11 encontrou a coluna ausente; após
autorização de Diogo, `CustoUnitarioCentavos bigint NULL` foi adicionada e sua
metadata verificada. Consulte
[CUSTO_PECAS.md](../backend/docs/CUSTO_PECAS.md). A persistência real do novo
campo por POST ainda não foi verificada; nenhum dado sintético foi inserido
durante a atualização do schema.
