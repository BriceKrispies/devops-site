#!/usr/bin/env bash
# Delegates to ci.sh — the single entrypoint for build + test + coverage.
# Kept for backwards compatibility.
exec "$(dirname "$0")/ci.sh" "$@"
