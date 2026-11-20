#!/usr/bin/env python3
"""Gera credenciais de development fora do Git; não sobrescreve configuração existente."""
import os
from pathlib import Path
import secrets

project_root = Path(__file__).resolve().parent.parent
env_path = project_root / ".env.local"
password = secrets.token_hex(24)
root_password = secrets.token_hex(24)
content = (
    "# Configuração LOCAL de development. Não versionar nem compartilhar.\n"
    "MYSQL_DATABASE=desanti_dev\n"
    "MYSQL_USER=desanti_app\n"
    "MYSQL_PORT=3306\n"
    f"MYSQL_PASSWORD={password}\n"
    f"MYSQL_ROOT_PASSWORD={root_password}\n"
    "ConnectionStrings__DefaultConnection='Server=127.0.0.1;Port=3306;"
    f"Database=desanti_dev;User ID=desanti_app;Password={password};"
    "SslMode=Disabled;AllowPublicKeyRetrieval=True'\n"
)
try:
    descriptor = os.open(env_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
except FileExistsError:
    print(".env.local já existe; conteúdo preservado.")
else:
    with os.fdopen(descriptor, "w") as file:
        file.write(content)
    print(".env.local criado com permissões 600; credenciais não exibidas.")
