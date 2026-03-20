#!/usr/bin/env bash
# Verify layer dependency rules.
# Exits non-zero if any violation is found.
#
# Dependency graph (→ = "may import from"):
#   platform  → types
#   state     → types
#   adapters  → state, types
#   effects   → state, platform, types, adapters
#   ui (non-entries) → state, types
#   ui/entries → all (composition root)

set -euo pipefail
cd "$(dirname "$0")/.."

ERRORS=0

check() {
  local layer="$1" pattern="$2" msg="$3"
  if grep -rn --include='*.ts' "$pattern" "src/$layer" 2>/dev/null | grep -v '\.d\.ts'; then
    echo "VIOLATION: $msg"
    ERRORS=$((ERRORS + 1))
  fi
}

# platform/ must not import ui, state, effects, or adapters
check "platform" 'from.*"\.\./ui/'       "platform/ imports from ui/"
check "platform" 'from.*"\.\./state/'    "platform/ imports from state/"
check "platform" 'from.*"\.\./effects/'  "platform/ imports from effects/"
check "platform" 'from.*"\.\./adapters/' "platform/ imports from adapters/"

# state/ must not import ui, effects, adapters, or platform
check "state" 'from.*"\.\./ui/'       "state/ imports from ui/"
check "state" 'from.*"\.\./effects/'  "state/ imports from effects/"
check "state" 'from.*"\.\./adapters/' "state/ imports from adapters/"
check "state" 'from.*"\.\./platform/' "state/ imports from platform/"

# adapters/ must not import ui or effects
check "adapters" 'from.*"\.\./ui/'       "adapters/ imports from ui/"
check "adapters" 'from.*"\.\./effects/'  "adapters/ imports from effects/"
check "adapters" 'from.*"\.\./platform/' "adapters/ imports from platform/"

# ui/ (excluding entries/) must not import adapters, effects, or platform
for f in $(find src/ui -name '*.ts' -not -path '*/entries/*' 2>/dev/null); do
  if grep -n 'from.*adapters/' "$f" 2>/dev/null; then
    echo "VIOLATION: $(basename "$f") in ui/ imports from adapters/"
    ERRORS=$((ERRORS + 1))
  fi
  if grep -n 'from.*effects/' "$f" 2>/dev/null; then
    echo "VIOLATION: $(basename "$f") in ui/ imports from effects/"
    ERRORS=$((ERRORS + 1))
  fi
  if grep -n 'from.*platform/' "$f" 2>/dev/null; then
    echo "VIOLATION: $(basename "$f") in ui/ imports from platform/"
    ERRORS=$((ERRORS + 1))
  fi
done

if [ "$ERRORS" -gt 0 ]; then
  echo ""
  echo "Found $ERRORS boundary violation(s)."
  exit 1
else
  echo "All layer boundaries OK."
fi
