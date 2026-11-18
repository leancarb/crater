#!/bin/bash
# Renderiza los planos en orden en <carpeta>/<plano>/####.png (se puede cortar y reanudar).
#   ./render_all.sh render s1 s2 s3 s5 s6 s7 s8 s9
cd "$(dirname "$0")"
OUT=${1:-render}; shift
for s in "$@"; do
  echo "== $s $(date +%T)"
  python -I crater_intro.py "$s" "$OUT" --samples "${SAMPLES:-8}"
done
