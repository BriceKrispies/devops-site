#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────
# Backend CI script — build, test, coverage
# Constitution §13.4: merge must fail when any check fails.
#
# Usage:
#   bash scripts/ci.sh            # default: Release build
#   bash scripts/ci.sh Debug      # override configuration
#
# Outputs:
#   artifacts/coverage/merged.cobertura.xml — machine-readable
#   artifacts/coverage/report/              — HTML report
#   exit code 0 on success, non-zero on any failure
# ──────────────────────────────────────────────────────────────
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$ROOT_DIR"

CONFIG="${1:-Release}"
ARTIFACTS="$ROOT_DIR/artifacts"
COVERAGE_DIR="$ARTIFACTS/coverage"
TEST_RESULTS="$ARTIFACTS/testresults"

BUILD_STATUS="passed"
TEST_STATUS="passed"
COVERAGE_PCT="N/A"

# ── Cleanup ──────────────────────────────────────────────────
rm -rf "$ARTIFACTS"
mkdir -p "$COVERAGE_DIR" "$TEST_RESULTS"

# ── Stage 1: Restore ────────────────────────────────────────
echo "::  Restoring..."
if ! dotnet restore DevOpsSite.sln --verbosity quiet > "$ARTIFACTS/restore.log" 2>&1; then
    BUILD_STATUS="FAILED"
    echo "FATAL: restore failed"
    cat "$ARTIFACTS/restore.log"
fi

# ── Stage 2: Build ──────────────────────────────────────────
if [ "$BUILD_STATUS" = "passed" ]; then
    echo "::  Building ($CONFIG)..."
    if ! dotnet build DevOpsSite.sln \
            --configuration "$CONFIG" \
            --no-restore \
            --verbosity quiet \
            -consoleloggerparameters:ErrorsOnly \
            > "$ARTIFACTS/build.log" 2>&1; then
        BUILD_STATUS="FAILED"
        echo ""
        echo "BUILD ERRORS:"
        cat "$ARTIFACTS/build.log"
        echo ""
    fi
fi

# ── Stage 3: Test + Coverage ────────────────────────────────
if [ "$BUILD_STATUS" = "passed" ]; then
    echo "::  Testing with coverage..."

    # Run tests. Capture full output to log, show only summary lines.
    set +e
    dotnet test DevOpsSite.sln \
        --configuration "$CONFIG" \
        --no-build \
        --results-directory "$TEST_RESULTS" \
        --collect:"XPlat Code Coverage" \
        --verbosity quiet \
        --logger "console;verbosity=minimal" \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
           DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Program.cs" \
        > "$ARTIFACTS/test.log" 2>&1
    TEST_EXIT=$?
    set -e

    # Show only the per-assembly summary lines and any failure lines
    grep -E "(Passed!|Failed!|Failed |Error )" "$ARTIFACTS/test.log" 2>/dev/null || true

    if [ $TEST_EXIT -ne 0 ]; then
        TEST_STATUS="FAILED"
        echo ""
        echo "TEST FAILURES — full log:"
        # Show lines around failures for context
        grep -B2 -A5 -E "(Failed |FAILED|X )" "$ARTIFACTS/test.log" 2>/dev/null || cat "$ARTIFACTS/test.log"
        echo ""
    fi
fi

# ── Stage 4: Aggregate Coverage ─────────────────────────────
if [ "$BUILD_STATUS" = "passed" ]; then
    echo "::  Generating coverage report..."

    dotnet tool restore > /dev/null 2>&1 || true

    # Collect all coverage files into a semicolon-separated list
    # Convert to Windows paths if running in Git Bash / MSYS (dotnet is a Windows binary)
    to_native_path() {
        if command -v cygpath > /dev/null 2>&1; then
            cygpath -w "$1"
        else
            echo "$1"
        fi
    }

    COVERAGE_FILES=""
    while IFS= read -r -d '' f; do
        native="$(to_native_path "$f")"
        if [ -n "$COVERAGE_FILES" ]; then
            COVERAGE_FILES="$COVERAGE_FILES;$native"
        else
            COVERAGE_FILES="$native"
        fi
    done < <(find "$TEST_RESULTS" -name "coverage.cobertura.xml" -print0 2>/dev/null)

    NATIVE_TARGET="$(to_native_path "$COVERAGE_DIR/report")"

    if [ -z "$COVERAGE_FILES" ]; then
        echo "  (no coverage files found)"
    else
        set +e
        dotnet reportgenerator \
            "-reports:$COVERAGE_FILES" \
            "-targetdir:$NATIVE_TARGET" \
            "-reporttypes:Cobertura;HtmlSummary;TextSummary" \
            "-verbosity:Warning" \
            > "$ARTIFACTS/reportgen.log" 2>&1
        RG_EXIT=$?
        set -e

        if [ $RG_EXIT -ne 0 ]; then
            echo "  (report generation failed — see artifacts/reportgen.log)"
            # Non-fatal: coverage report failure should not block the build
        fi

        # Copy merged Cobertura to predictable path
        if [ -f "$COVERAGE_DIR/report/Cobertura.xml" ]; then
            cp "$COVERAGE_DIR/report/Cobertura.xml" "$COVERAGE_DIR/merged.cobertura.xml"
        fi

        # Extract the overall line-rate from the merged Cobertura XML
        # Use sed (portable) instead of grep -P (not available on Git Bash)
        if [ -f "$COVERAGE_DIR/merged.cobertura.xml" ]; then
            LINE_RATE=$(sed -n 's/.*<coverage[^>]* line-rate="\([0-9.]*\)".*/\1/p' "$COVERAGE_DIR/merged.cobertura.xml" | head -1) || true
            if [ -n "${LINE_RATE:-}" ]; then
                COVERAGE_PCT=$(awk "BEGIN { printf \"%.2f%%\", $LINE_RATE * 100 }")
            fi
        fi

        # Print text summary
        if [ -f "$COVERAGE_DIR/report/Summary.txt" ]; then
            echo ""
            sed 's/^/  /' "$COVERAGE_DIR/report/Summary.txt"
        fi
    fi
fi

# ── Summary ──────────────────────────────────────────────────
echo ""
echo "========================================"
echo "  build:    $BUILD_STATUS"
echo "  tests:    $TEST_STATUS"
echo "  coverage: $COVERAGE_PCT"
echo "========================================"
echo ""

if [ "$BUILD_STATUS" = "passed" ] && [ "$TEST_STATUS" = "passed" ]; then
    echo "  artifacts/coverage/merged.cobertura.xml"
    echo "  artifacts/coverage/report/summary.htm"
    echo ""
    exit 0
else
    exit 1
fi
