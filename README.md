# DeSanti Automotiva

ERP para oficina automotiva, começando pela ordem de serviço. Estado técnico
revisado em **29/10/2026**; os documentos de 02/10 e o repasse de 05/10 representam
etapas anteriores do projeto.

O Flutter seleciona um cliente cadastrado e envia a OS para a API ASP.NET Core.
O backend valida os dados, preserva o histórico do cliente, calcula o total em
centavos e implementa a gravação de OS e peças numa transaction MySQL. A
persistência real foi verificada no MySQL local em 29/10/2026, incluindo rollback
e preservação do histórico. A suíte automatizada usa fakes e geração de SQL em
memória; o check real é executado separadamente e exige autorização para seus
dados fictícios.

- [Frontend: execução e estrutura MVVM](frontend/README.md)
- [Backend: layers, banco local e comandos](backend/README.md)
- [Integração OS: API contract e explicação do código](backend/docs/INTEGRACAO_OS.md)
- [Diretrizes e decisões confirmadas](AGENTS.md)
- [Repasse histórico de 05/10/2026](REPASSE_THALLES.md)

## Desenvolvimento local

Na raiz do repositório:

```bash
python3 scripts/configure_dev.py
sudo docker compose --env-file .env.local -f compose.dev.yaml up -d --wait
./scripts/dev-api.sh
```

Em outro terminal:

```bash
cd frontend
flutter run -d linux --dart-define=API_BASE_URL=http://localhost:5085
```

O Compose inicia MySQL 8.4 LTS restrito a `127.0.0.1`, com volume persistente e
schema inicial aplicado apenas num volume vazio. `.env.local` contém credenciais
locais geradas, está ignorado pelo Git e não deve ser compartilhado. A API usa um
usuário de aplicação e não executa migrations no startup.

Cadastro de clientes na tela, listagem/detalhe e finalização de OS, Falcon, PDF,
WhatsApp e hosting de produção ficam para etapas posteriores. A divisão atual
de trabalho com Thalles deve ser confirmada antes de atribuir tarefas.
