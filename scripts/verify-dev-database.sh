#!/usr/bin/env bash
# Execução explícita: grava e remove SOMENTE dados sintéticos no banco local de development.
set -euo pipefail
desanti_project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ ! -f "$desanti_project_root/.env.local" ]]; then
  echo 'Execute python3 scripts/configure_dev.py antes da verificação local.' >&2
  exit 1
fi
set -a
source "$desanti_project_root/.env.local"
set +a
exec dotnet run --file "$desanti_project_root/scripts/verify-dev-database.cs"
