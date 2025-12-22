set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR=${OUTPUT_DIR:-"$ROOT/output"}
CONCURRENCY=${CONCURRENCY:-1}
THRESHOLD=${THRESHOLD:-0.5}

mkdir -p "$OUTPUT_DIR"
shopt -s nullglob
files=("$ROOT"/inputs/participants*.json)

if [ ${#files[@]} -eq 0 ]; then
  echo "no inputs/participants*.json files found."
  exit 0
fi

for f in "${files[@]}"; do
  base="$(basename "$f" .json)"
  out="$OUTPUT_DIR/${base}-results-top30.csv"
  echo "processing $f -> $out"
  "$ROOT/run.sh" \
    --input "$f" \
    --outputCsv "$out" \
    --concurrency "$CONCURRENCY" \
    --threshold "$THRESHOLD"
done
