# DeSanti Automotiva — repasse de trabalho

Atualizado em 05/10/2026. Este resumo divide o trabalho por funcionalidade
fullstack para que as telas de Clientes e Pedidos avancem em paralelo.

## Estado geral

- ✅ O app Flutter já tem um formulário inicial de Ordem de Serviço (OS), com
  cliente, veículo, descrição do serviço, peças, mão de obra, total e retorno.
- ✅ O formulário aplica máscaras e calcula dinheiro em centavos. O Nome é
  validado ao conferir mesmo com o botão no fim da tela; a lista usa
  `SingleChildScrollView` para manter os campos montados.
- ✅ O Flutter monta `CriarOrdemServicoRequest`, mas ainda não chama a API nem
  salva a OS.
- ✅ A API ASP.NET Core .NET 10 tem request DTO e validador. Quatro testes do
  validador passaram.
- 🟡 A funcionalidade de Pedidos/OS está em andamento: a base do formulário e
  do contrato existe; falta persistência, rota e integração front/back.
- ⬜ A tela e a API de Clientes ainda não começaram.
- ⬜ MySQL está escolhido como tecnologia, mas falta decidir se será local ou
  na Umbler. O SQL está em rascunho e ainda não foi executado.

## Divisão fullstack sugerida

### Thalles — Clientes (frontend + backend)

Responsável pela funcionalidade completa de Clientes:

- Flutter: criar `frontend/lib/features/clientes/` com tela de listagem,
  busca e cadastro/edição de clientes.
- ASP.NET: criar `backend/DeSanti.Api/Clientes/` com modelo, validação e rotas
  de clientes, começando por listagem, busca e criação/edição.
- Testes: adicionar testes de validação e das rotas em
  `backend/DeSanti.Api.Tests/`.
- Contrato sugerido: `GET /api/clientes`, `GET /api/clientes/{id}`,
  `POST /api/clientes` e `PUT /api/clientes/{id}`. Definir regra de exclusão
  para clientes que já tenham OS antes de adicionar `DELETE`.

### Você — Pedidos/OS (frontend + backend)

Responsável pela funcionalidade completa de Pedidos/OS:

- Flutter: continuar em `frontend/lib/features/ordens_servico/`; a tela já
  monta o request, mas precisa selecionar um cliente cadastrado e enviar a OS
  para a API.
- ASP.NET: continuar em `backend/DeSanti.Api/OrdensServico/`; implementar
  persistência transacional de OS e peças, cálculo do total em centavos no
  servidor, e `POST /api/ordens-servico`. Depois incluir listagem/detalhe e
  finalização da OS.
- Testes: ampliar `backend/DeSanti.Api.Tests/` para persistência, totais,
  validação e rotas.
- Retorno: guardar prazo na criação; deixar conclusão e previsão nulas. Ao
  finalizar, registrar a conclusão e calcular a data prevista de retorno.

### Trabalho compartilhado

- Definir o contrato Clientes–OS antes de integrar: a OS deve referenciar um
  `clienteId`; decidir também se guarda uma cópia dos dados do cliente no
  momento da abertura para preservar o histórico.
- Rever o SQL atual antes de executá-lo. O rascunho
  `backend/DeSanti.Api/Database/001_criar_ordens_servico.sql` grava nome, CPF e
  telefone dentro da OS e ainda não tem tabela `clientes` nem `cliente_id`.
- Criar configuração da conexão MySQL fora do app e sem versionar credenciais.
  A API é a única camada que conversa com o banco.
- Integrar as rotas à navegação em `frontend/lib/main.dart` e combinar os
  formatos de erro, sucesso e identificadores.
- O repositório não tem histórico Git local. Preparar Git e combinar branches
  ou ordem de integração antes de editar os mesmos arquivos em paralelo.

## Arquivos e situação técnica

- Frontend OS: `frontend/lib/features/ordens_servico/nova_ordem_page.dart`,
  `pecas_section.dart` e `criar_ordem_servico_request.dart`.
- Backend OS: `backend/DeSanti.Api/OrdensServico/CriarOrdemServicoRequest.cs`
  e `CriarOrdemServicoValidator.cs`.
- Banco: `backend/DeSanti.Api/Database/001_criar_ordens_servico.sql` é somente
  rascunho. A dependência `MySqlConnector` já está no projeto. Não há conexão
  configurada nem banco inicializado.
- `backend/DeSanti.Api/Program.cs` ainda contém o endpoint de exemplo
  `/weatherforecast`; não existe endpoint de OS nem de Clientes.
- O projeto compila e os quatro testes de validação passam. `flutter analyze`
  deixa um aviso informativo de import desnecessário em
  `frontend/lib/shared/formatters/money_input_formatter.dart`.

## Próxima ordem de trabalho

1. Criar/confirmar Git e acordar a interface de dados entre Cliente e OS.
2. Thalles inicia Clientes fullstack; você ajusta o contrato da OS para receber
   `clienteId` e começa o repositório/rota de Pedidos.
3. Definir MySQL local ou Umbler, atualizar o esquema com tabela `clientes` e
   relação com OS, configurar a conexão e então executar o SQL.
4. Integrar e validar o fluxo completo: cadastrar cliente → abrir OS com esse
   cliente → persistir OS e peças → consultar OS.

## Comandos de verificação

Na raiz do projeto:

```bash
dotnet build backend/DeSanti.Api/DeSanti.API.csproj
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj
cd frontend && flutter analyze
```
