# Backend da DeSanti Automotiva

API ASP.NET Core em .NET 10. O projeto está em `DeSanti.Api/` e os testes em
`DeSanti.Api.Tests/`.

## Implementado

- `OrdensServico/CriarOrdemServicoRequest.cs`: contrato da criação de OS e das
  peças. Valores monetários chegam como inteiros em centavos.
- `OrdensServico/CriarOrdemServicoValidator.cs`: valida nome, CPF (inclusive
  dígitos verificadores), celular com DDD, placa, veículo, ano opcional,
  descrição, peças, mão de obra e coerência do prazo de retorno.
- `DeSanti.Api.Tests/UnitTest1.cs`: quatro testes permanentes do validador com
  dados sintéticos.
- `Database/001_criar_ordens_servico.sql`: tabelas `ordens_servico` e
  `pecas_ordem_servico`, ligadas por chave estrangeira. Conclusão e previsão de
  retorno são colunas opcionais; devem ficar nulas na criação.
- Dependência `MySqlConnector` 2.6.2 adicionada ao projeto da API.

## Ainda não implementado

`Program.cs` conserva a rota `/weatherforecast` do template. Ainda não há
endpoint de OS, conexão configurada, repositório de dados, gravação no MySQL
nem migração aplicada. O script SQL deve ser executado em um banco escolhido
antes de testar a persistência. Não há credenciais no repositório.

## Comandos

Na raiz do projeto:

```bash
dotnet build backend/DeSanti.Api/DeSanti.API.csproj
dotnet test backend/DeSanti.Api.Tests/DeSanti.Api.Tests.csproj
```

Na última verificação, o build passou sem avisos ou erros e os quatro testes
passaram. A próxima etapa é configurar MySQL local ou da Umbler, salvar OS e
peças em uma transação, calcular o total na API e expor
`POST /api/ordens-servico`. Conclusão do serviço e cálculo da data de retorno
serão implementados depois da criação da OS.
