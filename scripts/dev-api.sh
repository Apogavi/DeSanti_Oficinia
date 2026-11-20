#!/usr/bin/env bash
# Carrega somente a configuração local e inicia a API; não executa migrations.
set -euo pipefail
desanti_project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ ! -f "$desanti_project_root/.env.local" ]]; then
  echo 'Execute python3 scripts/configure_dev.py antes de iniciar a API local.' >&2
  exit 1
fi
set -a
source "$desanti_project_root/.env.local"
set +a
exec dotnet run --project "$desanti_project_root/backend/DeSanti.Api/DeSanti.API.csproj" --launch-profile http
