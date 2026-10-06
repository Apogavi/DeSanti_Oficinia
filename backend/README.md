# Backend da DeSanti Automotiva

API ASP.NET Core em .NET 10. A organização segue as quatro camadas usadas no
backend .NET do IvoRosa_ERP, adaptadas ao estágio atual da oficina e ao MySQL:

| Projeto | Responsabilidade atual |
| --- | --- |
| `DeSanti.Api` | Entrada HTTP, configuração e futura exposição dos controllers. |
| `DeSanti.Application` | Contrato da criação de OS, validação e abstração da conexão de dados. |
| `DeSanti.Domain` | Projeto reservado para entidades e regras de negócio da oficina. Ainda não contém entidades: a persistência da OS não foi implementada. |
| `DeSanti.Infrastructure` | Fábrica de conexões MySQL e script SQL inicial. Repositórios serão acrescentados quando houver persistência. |
| `DeSanti.Api.Tests` | Testes da validação da OS. |

As dependências seguem `Api → Application + Infrastructure`,
`Infrastructure → Application + Domain` e `Application → Domain`. O domínio não
depende dos demais projetos. A solução está em `DeSanti.slnx`.

## Implementado

- `DeSanti.Application/DTOs/OrdensServico/CriarOrdemServicoRequest.cs` mantém o
  contrato existente da OS e das peças. Valores monetários chegam como inteiros
  em centavos.
- `DeSanti.Application/Validators/OrdensServico/CriarOrdemServicoValidator.cs`
  valida nome, CPF (inclusive dígitos verificadores), celular com DDD, placa,
  veículo, ano opcional, descrição, peças, mão de obra e prazo de retorno.
- `DeSanti.Infrastructure/Data/MySqlConnectionFactory.cs` cria conexões com
  `MySqlConnector`, usando `ConnectionStrings:DefaultConnection`. A conexão só é
  aberta quando um futuro repositório a solicitar; iniciar a API não depende de
  um banco configurado.
- `DeSanti.Infrastructure/Data/Scripts/001_criar_ordens_servico.sql` define as
  tabelas `ordens_servico` e `pecas_ordem_servico`. O script ainda não foi
  executado. Conclusão e previsão de retorno são opcionais na criação.
- `DeSanti.Api.Tests` mantém os quatro testes da validação com dados sintéticos.

## Ainda não implementado

Não há endpoint de OS, serviço de aplicação para criação, repositório de dados,
gravação no MySQL nem migração aplicada. A API está preparada para controllers,
mas nenhum foi publicado ainda. A estrutura do IvoRosa usa EF Core/PostgreSQL;
esse stack de persistência não foi copiado, pois a DeSanti usa MySQL e já possui
um script e `MySqlConnector`. Também não foram importados módulos de negócio do
outro ERP.

## Configuração e comandos

Quando o acesso ao banco for implementado, configure a string de conexão fora
do repositório, por exemplo com a variável de ambiente
`ConnectionStrings__DefaultConnection`. Exemplo **fictício**:

```text
Server=localhost;Port=3306;Database=desanti_exemplo;User ID=usuario_exemplo;Password=senha_exemplo
```

Na raiz do projeto:

```bash
dotnet build backend/DeSanti.slnx
dotnet test backend/DeSanti.slnx
```

A próxima etapa funcional é configurar o MySQL local ou da Umbler, persistir a
OS e suas peças em uma transação, calcular o total na API e expor
`POST /api/ordens-servico`. Conclusão do serviço e cálculo da data de retorno
virão depois da criação da OS.
