#!/bin/bash
# Renderiza los planos de la segunda apertura (solo los cuatro kits) en <carpeta>/<plano>/####.png.
#   ./render_v2.sh render2 v1 v2 v3 v4 v5 v6 v7 v8
cd "$(dirname "$0")"
OUT=${1:-render2}; shift
for s in "$@"; do
  find "$OUT/$s" -maxdepth 1 -name '*.png' -size 0 -delete 2>/dev/null   # placeholders de un corte
  echo "== $s $(date +%T)"
  python -I crater_v2.py "$s" "$OUT" --samples "${SAMPLES:-8}"
done
