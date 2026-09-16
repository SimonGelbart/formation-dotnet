#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
OUT="$ROOT/index.html"
{
  cat "$ROOT/_base.html"
  for module in "$ROOT"/modules/*.html; do
    cat "$module"
  done
  cat "$ROOT/_footer.html"
} > "$OUT"
printf 'Cours généré : %s\n' "$OUT"
