#!/bin/sh
# Imprime o bloco de segredos da observabilidade, com valores aleatórios.
# Uso: sh observabilidade/gerar-segredos.sh >> .env
set -e

senha() { openssl rand -hex 16; }

cat <<EOF

# --- Observabilidade (gerado por observabilidade/gerar-segredos.sh) ---
LANGFUSE_DB_PASSWORD=$(senha)
LANGFUSE_CLICKHOUSE_PASSWORD=$(senha)
LANGFUSE_MINIO_PASSWORD=$(senha)
LANGFUSE_REDIS_PASSWORD=$(senha)
LANGFUSE_SALT=$(senha)
LANGFUSE_ENCRYPTION_KEY=$(openssl rand -hex 32)
LANGFUSE_NEXTAUTH_SECRET=$(openssl rand -hex 32)
LANGFUSE_PROJECT_PUBLIC_KEY=pk-lf-$(openssl rand -hex 16)
LANGFUSE_PROJECT_SECRET_KEY=sk-lf-$(openssl rand -hex 16)
LANGFUSE_ADMIN_EMAIL=admin@invoisys.local
LANGFUSE_ADMIN_PASSWORD=$(senha)
GRAFANA_ADMIN_PASSWORD=$(senha)
EOF
