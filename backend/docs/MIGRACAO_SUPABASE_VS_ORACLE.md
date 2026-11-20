# Portabilidade de Banco: Oracle Cloud Free VPS (MySQL) vs. Supabase (PostgreSQL)

Guia de referência criado em **19/11/2026** (`[Antigravity / Gemini 4 Neon]`) para apoiar a decisão de infraestrutura de `production` da DeSanti Automotiva.

## 1. Como o Backend está preparado hoje

Todo o código de `DeSanti.Domain`, `DeSanti.Application` e consultas de `ConsultaOrdemRepository` / `OrdemServicoRepository` / `ClienteRepository` usa **EF Core LINQ padrão** (sem SQL raw específico de MySQL).

O único acoplamento ao MySQL está isolado em `DeSanti.Infrastructure`:
1. Pacote NuGet `MySql.EntityFrameworkCore` 10.0.12 em `DeSanti.Infrastructure.csproj`.
2. Chamada `options.UseMySQL(connectionString)` em `DeSanti.Infrastructure/DependencyInjection.cs`.
3. Captura de violação de índice único (`MySqlException { Number: 1062 }`) em `ClienteRepository.cs`.

## 2. Comparativo Prático para a Oficina

| Critério | Oracle Cloud Free VPS + MySQL | Supabase (PostgreSQL gerenciado) |
| :--- | :--- | :--- |
| **Alteração de código** | **Zero** (mesmo provider MySQL atual). | **Pequena** (~3 arquivos na `Infrastructure` para trocar para `Npgsql.EntityFrameworkCore.PostgreSQL` e código de erro `23505`). |
| **Backup e manutenção** | Exige configurar `cron` + `scripts/backup-mysql.sh` e manter o SO Linux da VPS. | Backups e painel gerenciados, mas o plano gratuito pausa projeto após 7 dias sem atividade. |
| **Hospedagem da API .NET** | Pode rodar a própria API .NET + Nginx/HTTPS + MySQL na mesma VPS ARM/AMD gratuita. | Supabase hospeda apenas o banco (PostgreSQL); a API .NET ainda precisaria rodar no PC da oficina ou em outro host. |
| **Custo** | Gratuito (Always Free Oracle, sujeito a validação de cartão e política de conta). | Gratuito até 500 MB (atenção à pausa por inatividade no plano free). |

## 3. Passos caso decidam migrar para Supabase (PostgreSQL) no futuro

1. Em `DeSanti.Infrastructure.csproj`, substituir `MySql.EntityFrameworkCore` por `Npgsql.EntityFrameworkCore.PostgreSQL` (versão compatível com .NET 10 / EF Core 10).
2. Em `DeSanti.Infrastructure/DependencyInjection.cs`, trocar `options.UseMySQL(connectionString)` por `options.UseNpgsql(connectionString)`.
3. Em `ClienteRepository.cs`, trocar o filtro de violação de chave única para `PostgresException { SqlState: "23505" }`.
4. Aplicar o schema inicial e as evoluções com a sintaxe PostgreSQL (já documentada em `backend/docs/sql/2026-11-19_orcamento_e_idempotency.proposta.sql`).
5. Configurar a connection string do Supabase (Transaction/Session Pooler com SSL) fora do Git.
