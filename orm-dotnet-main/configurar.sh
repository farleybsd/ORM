#!/usr/bin/env bash
# Copia o .env da raiz para todas as pastas de capítulo.
# Uso (Linux/Mac, na pasta do repositório): ./configurar.sh
cd "$(dirname "$0")"
if [ ! -f .env ]; then
  cp .env.example .env
  echo "Criei o .env a partir do .env.example. Confira a senha do banco e rode de novo."
  exit 0
fi
for d in cap*/; do
  if [ -f "$d.env.example" ]; then
    cp .env "$d.env"
    echo "  .env -> $d"
  fi
done
echo "Pronto."
