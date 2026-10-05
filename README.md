# DeSanti Automotiva

ERP para oficina automotiva, começando pela ordem de serviço (OS). Este arquivo
registra o estado técnico conferido em 02/10/2026. Para divisão de trabalho
fullstack entre Pedidos e Clientes, consulte `REPASSE_THALLES.md`. As instruções
de colaboração e os requisitos permanentes estão em `AGENTS.md`.

## Estado atual

- `frontend/`: aplicativo Flutter para Linux durante o desenvolvimento e Windows
  na oficina. O formulário da OS coleta cliente, veículo, descrição, peças, mão de
  obra e prazo de retorno. Aplica máscaras e calcula valores em centavos.
- O botão **Conferir dados** valida todos os campos e monta um
  `CriarOrdemServicoRequest` com CPF e telefone sem máscara e placa sem hífen.
  Ele ainda **não envia nem salva** a OS.
- A lista do formulário usa `SingleChildScrollView` com `Column` para manter
  todos os campos montados. Isso corrigiu o caso em que o campo Nome não era
  validado quando o botão era acionado no fim da página.
- `backend/DeSanti.Api/`: API ASP.NET Core em .NET 10. Há DTO de criação e
  validador para dados da OS. `Program.cs` ainda contém somente a rota de
  exemplo `/weatherforecast` do template; não há rota de OS.
- `backend/DeSanti.Api.Tests/`: quatro testes do validador (pedido válido, nome
  vazio, CPF inválido e retorno sem prazo).
- `backend/DeSanti.Api/Database/001_criar_ordens_servico.sql`: esquema MySQL
  planejado para OS e peças. O script ainda **não foi executado** e nenhum banco
  está conectado à API. O pacote `MySqlConnector` já foi adicionado ao projeto.

## Decisões de arquitetura

O Flutter acessará dados somente pela API ASP.NET; credenciais do banco ficam
fora do aplicativo. O banco será MySQL, local ou na Umbler; o local definitivo
ainda não foi escolhido. App e API serão executados em um PC Windows na oficina.
O desenvolvimento ocorre no Omarchy. A API precisa aceitar endereço de banco
por configuração, sem senhas versionadas.

Os valores monetários são inteiros em centavos. A API deve validar o pedido e
recalcular o total antes de gravar a OS e suas peças em uma transação. A data
prevista de retorno permanece vazia na criação; só será calculada ao finalizar
o serviço, a partir da data de conclusão.

Consulta por placa com Falcon Data Hub, PDF e compartilhamento por WhatsApp são
etapas futuras. A origem do campo de ano da consulta (fabricação ou ano modelo)
ainda precisa ser definida.

## Verificação na pausa

Execute os comandos a partir da raiz, exceto o `flutter analyze`:

```bash
dotnet build backend/DeSanti.Api/DeSanti.API.csproj
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj
cd frontend && flutter analyze
```

Em 02/10/2026, o build da API passou sem avisos ou erros e os quatro testes
passaram. O `flutter analyze` encontrou somente um aviso informativo: import
desnecessário de `package:flutter/material.dart` em
`frontend/lib/shared/formatters/money_input_formatter.dart`.

## Ponto de retomada

1. Revisar o esquema SQL e definir um MySQL de desenvolvimento; executar o
   script em um banco de teste. Ainda não há cliente/servidor MySQL configurado
   neste projeto.
2. Configurar a conexão da API por variável de ambiente ou configuração local
   não versionada. Implementar a gravação transacional da OS e das peças, com
   cálculo do total no servidor e tratamento de estouro de inteiros.
3. Criar `POST /api/ordens-servico`, retornando erros de validação e o ID da OS
   criada. Testar com dados fictícios, sem CPF, telefone ou placa reais.
4. Ligar o Flutter à rota com endereço da API configurável; mostrar sucesso ou
   falha de gravação e documentar o fluxo.

O repositório ainda não tem histórico Git local. Antes de versionar, preparar
um `.gitignore` para artefatos de Flutter e .NET e excluir configurações locais
com credenciais.
