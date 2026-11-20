#!/usr/bin/env bash
# Gera backup lógico (.sql.gz) do banco MySQL configurado em .env.local (ou variáveis de ambiente).
# Não executa alterações de schema nem restaurações. Uso previsto para rotina de backup/homologação.
set -euo pipefail

desanti_project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
backup_dir="${DESANTI_BACKUP_DIR:-$desanti_project_root/backups}"
retencao_dias="${DESANTI_BACKUP_RETENTION_DAYS:-14}"

if [[ -f "$desanti_project_root/.env.local" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "$desanti_project_root/.env.local"
  set +a
fi

: "${MYSQL_DATABASE:?Defina MYSQL_DATABASE ou configure .env.local}"
: "${MYSQL_USER:?Defina MYSQL_USER ou configure .env.local}"
: "${MYSQL_PASSWORD:?Defina MYSQL_PASSWORD ou configure .env.local}"
mysql_host="${MYSQL_HOST:-127.0.0.1}"
mysql_port="${MYSQL_PORT:-3306}"

mkdir -p "$backup_dir"
chmod 700 "$backup_dir"

timestamp="$(date +%Y%m%d_%H%M%S)"
arquivo="$backup_dir/${MYSQL_DATABASE}_${timestamp}.sql.gz"

MYSQL_PWD="$MYSQL_PASSWORD" mysqldump \
  --host="$mysql_host" \
  --port="$mysql_port" \
  --user="$MYSQL_USER" \
  --single-transaction \
  --quick \
  --routines \
  --triggers \
  "$MYSQL_DATABASE" | gzip -9 > "$arquivo"

chmod 600 "$arquivo"
echo "Backup concluído: $arquivo"

# Remove backups mais antigos que o período de retenção configurado.
find "$backup_dir" -maxdepth 1 -type f -name "${MYSQL_DATABASE}_*.sql.gz" -mtime "+$retencao_dias" -delete
