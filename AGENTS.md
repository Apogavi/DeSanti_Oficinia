# Repository Guidelines

## Produto

Construir um ERP para uma oficina automotiva, começando pela ordem de serviço (OS). Ela registra cliente (nome, CPF, telefone), veículo (placa, marca, modelo, cor e ano opcional), descrição do serviço, peças separadas (nome, quantidade e valor unitário), mão de obra e total. CPF, celular e placa recebem máscaras; o ano aceita até quatro dígitos. Valores monetários são calculados em centavos e formatados durante a digitação (`12000` → `120,00`). O retorno é indicado por checkbox e prazo em dias. A data prevista de retorno só é calculada ao finalizar o serviço, a partir da data de conclusão; antes disso, fica vazia. PDF e compartilhamento por WhatsApp são etapas posteriores.

## Estrutura e arquitetura

`frontend/` contém o Flutter/Dart: `lib/main.dart` inicia o app, `lib/theme/` guarda o tema e `lib/features/ordens_servico/` contém o formulário e as peças. `backend/DeSanti.Api/` contém a API ASP.NET em desenvolvimento. Desenvolvimento: Omarchy e MySQL local. Produção: app e API em um PC Windows; o banco será MySQL na Umbler ou localmente, decisão ainda pendente. O Flutter acessa banco e serviços externos somente pela API local; credenciais ficam fora do aplicativo. Compile o alvo Windows em uma máquina Windows.

O acesso por celular é uma evolução possível. Mantenha o endereço da API configurável e a interface adaptável a telas menores. Na oficina, o celular poderá acessar a API pela rede local; fora dela, será necessário acesso remoto seguro ou hospedar a API publicamente com HTTPS e autenticação. GoDaddy é um candidato de hospedagem, ainda sem decisão. Nunca conectar o cliente móvel diretamente ao banco.

## Consulta por placa

A integração fica para uma etapa futura, usando a Falcon Data Hub (conta de desenvolvimento criada). Ao informar uma placa válida, a API ASP.NET consultará o fornecedor e sugerirá marca, modelo, cor e ano, sempre editáveis. Normalizar a placa sem hífen e evitar chamadas a cada tecla; preservar edição manual se a consulta falhar. O retorno observado contém `data.marca`, `data.modelo`, `data.cor`, `data.ano` e `data.ano_modelo`. Definir se o campo “ano” usará fabricação ou ano modelo antes de mapear a resposta. Não versionar o token da Falcon.

## Desenvolvimento, estilo e testes

Em `frontend/`, execute `flutter pub get`, `flutter run -d linux` e `flutter analyze`. No backend, execute `dotnet build backend/DeSanti.Api/DeSanti.API.csproj` e `dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj` a partir da raiz. Separe interface, modelos e acesso a dados; use as convenções e formatadores de Dart e C#. Estado local do formulário pode usar `StatefulWidget`; avalie Riverpod quando listas e chamadas à API forem compartilhadas. Ao adicionar regras de negócio, crie testes úteis e documente os comandos.

## Dados e configuração

Colete apenas os dados necessários para a OS. Não use CPF, telefone ou placa reais em testes, logs e exemplos. Nunca versione senhas, tokens ou configuração local do banco; forneça exemplos fictícios de variáveis de ambiente.

## Colaboração e documentação

O usuário escreve manualmente o código do app no VS Code. Forneça caminho completo e trechos exatos; não edite código do app sem pedido explícito. Atualize este `AGENTS.md` diretamente quando requisitos ou processo mudarem. Documente cada funcionalidade em `frontend/README.md` ou no local apropriado.

O estado técnico, a divisão fullstack entre Clientes e Pedidos e o ponto de retomada estão em `REPASSE_THALLES.md`. Detalhes da API, testes e esquema SQL estão em `backend/README.md`. O esquema ainda não foi executado; a API ainda usa o `Program.cs` padrão e não grava OS.

## Commits e pull requests

Não há histórico Git local para inferir convenções. Use commits pequenos com títulos imperativos. Em pull requests, descreva objetivo, impacto e validações; inclua capturas para mudanças visuais.
