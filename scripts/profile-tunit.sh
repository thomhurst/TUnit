#!/usr/bin/env bash
#
# profile-tunit.sh — Build and profile a TUnit test project using dotnet-trace and dotnet-counters.
#
# Produces:
#   <output-dir>/trace.nettrace      — Full execution trace (open in PerfView, VS, or speedscope)
#   <output-dir>/trace.speedscope    — Speedscope JSON (open at https://speedscope.app)
#   <output-dir>/counters.csv        — Runtime counters (GC, threadpool, CPU, etc.)
#   <output-dir>/dump.dmp            — (optional) Full memory dump for heap analysis
#
# Prerequisites:
#   dotnet tool install -g dotnet-trace
#   dotnet tool install -g dotnet-counters
#   dotnet tool install -g dotnet-dump       (optional, for --dump)
#
# Usage:
#   ./scripts/profile-tunit.sh [options]
#
# Examples:
#   # Profile all tests in TUnit.TestProject (WARNING: many are designed to fail)
#   ./scripts/profile-tunit.sh
#
#   # Profile specific tests with a filter
#   ./scripts/profile-tunit.sh --filter "/*/*/BasicTests/*"
#
#   # Profile a different test project
#   ./scripts/profile-tunit.sh --project TUnit.PerformanceBenchmarks
#
#   # Profile with a memory dump captured mid-run
#   ./scripts/profile-tunit.sh --filter "/*/*/BasicTests/*" --dump
#
#   # Use a specific framework
#   ./scripts/profile-tunit.sh --framework net9.0

set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="TUnit.TestProject"
FRAMEWORK="net10.0"
CONFIGURATION="Release"
FILTER=""
OUTPUT_DIR=""
COLLECT_DUMP=false
TRACE_PROFILE="cpu-sampling"  # cpu-sampling | gc-verbose | gc-collect | none
COUNTERS_INTERVAL=1           # seconds between counter snapshots
TRACE_FORMAT="speedscope"     # speedscope | chromium | nettrace
EXTRA_ARGS=()

# ── Usage ─────────────────────────────────────────────────────────────────────

usage() {
    cat <<'USAGE'
Usage: profile-tunit.sh [options] [-- extra-test-args...]

Options:
  --project <name>         Test project to profile (default: TUnit.TestProject)
  --framework <tfm>        Target framework (default: net10.0)
  --configuration <cfg>    Build configuration (default: Release)
  --filter <treenode>      Test treenode filter (e.g. "/*/*/BasicTests/*")
  --output <dir>           Output directory (default: .profile/<project>-<timestamp>)
  --trace-profile <p>      Trace profile: cpu-sampling, gc-verbose, gc-collect, none (default: cpu-sampling)
  --trace-format <f>       Trace export format: speedscope, chromium, nettrace (default: speedscope)
  --counters-interval <s>  Counter collection interval in seconds (default: 1)
  --dump                   Also capture a memory dump during test execution
  --no-build               Skip the build step (use existing build output)
  --help                   Show this help

Everything after '--' is passed directly to the test executable.
USAGE
    exit 0
}

# ── Parse arguments ───────────────────────────────────────────────────────────

SKIP_BUILD=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --project)        PROJECT="$2"; shift 2 ;;
        --framework)      FRAMEWORK="$2"; shift 2 ;;
        --configuration)  CONFIGURATION="$2"; shift 2 ;;
        --filter)         FILTER="$2"; shift 2 ;;
        --output)         OUTPUT_DIR="$2"; shift 2 ;;
        --trace-profile)  TRACE_PROFILE="$2"; shift 2 ;;
        --trace-format)   TRACE_FORMAT="$2"; shift 2 ;;
        --counters-interval) COUNTERS_INTERVAL="$2"; shift 2 ;;
        --dump)           COLLECT_DUMP=true; shift ;;
        --no-build)       SKIP_BUILD=true; shift ;;
        --help)           usage ;;
        --)               shift; EXTRA_ARGS=("$@"); break ;;
        *)                echo "Unknown option: $1"; usage ;;
    esac
done

# ── Resolve paths ─────────────────────────────────────────────────────────────

PROJECT_DIR="$REPO_ROOT/$PROJECT"
if [[ ! -d "$PROJECT_DIR" ]]; then
    echo "ERROR: Project directory not found: $PROJECT_DIR"
    exit 1
fi

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
if [[ -z "$OUTPUT_DIR" ]]; then
    OUTPUT_DIR="$REPO_ROOT/.profile/${PROJECT}-${TIMESTAMP}"
fi
mkdir -p "$OUTPUT_DIR"

# Resolve the built executable path
if [[ "$(uname -o 2>/dev/null || true)" == "Msys" || "$(uname -s)" == MINGW* || "$(uname -s)" == CYGWIN* ]]; then
    EXE_NAME="${PROJECT}.exe"
else
    EXE_NAME="$PROJECT"
fi
EXE_PATH="$PROJECT_DIR/bin/$CONFIGURATION/$FRAMEWORK/$EXE_NAME"

echo "═══════════════════════════════════════════════════════════════════"
echo "  TUnit Profiler"
echo "═══════════════════════════════════════════════════════════════════"
echo "  Project:       $PROJECT"
echo "  Framework:     $FRAMEWORK"
echo "  Configuration: $CONFIGURATION"
echo "  Filter:        ${FILTER:-<none - all tests>}"
echo "  Trace profile: $TRACE_PROFILE"
echo "  Output:        $OUTPUT_DIR"
echo "═══════════════════════════════════════════════════════════════════"
echo ""

# ── Step 1: Build ─────────────────────────────────────────────────────────────

if [[ "$SKIP_BUILD" == false ]]; then
    echo "▶ Building $PROJECT ($CONFIGURATION | $FRAMEWORK)..."
    dotnet build "$PROJECT_DIR" \
        -c "$CONFIGURATION" \
        -f "$FRAMEWORK" \
        --nologo \
        -v quiet \
        -p:TreatWarningsAsErrors=false
    echo "  ✓ Build complete"
    echo ""
else
    echo "▶ Skipping build (--no-build)"
    echo ""
fi

if [[ ! -f "$EXE_PATH" ]]; then
    echo "ERROR: Executable not found at: $EXE_PATH"
    echo "       Try building without --no-build, or check --framework and --configuration."
    exit 1
fi

# ── Build test command ────────────────────────────────────────────────────────

TEST_CMD=("$EXE_PATH")
if [[ -n "$FILTER" ]]; then
    TEST_CMD+=("--treenode-filter" "$FILTER")
fi
if [[ ${#EXTRA_ARGS[@]} -gt 0 ]]; then
    TEST_CMD+=("${EXTRA_ARGS[@]}")
fi

# ── Step 2: dotnet-trace ──────────────────────────────────────────────────────

TRACE_FILE="$OUTPUT_DIR/trace.nettrace"

if [[ "$TRACE_PROFILE" != "none" ]]; then
    echo "▶ Collecting trace (profile: $TRACE_PROFILE)..."

    TRACE_ARGS=(
        collect
        --output "$TRACE_FILE"
        --profile "$TRACE_PROFILE"
        --format "NetTrace"  # always collect as nettrace first
        --
        "${TEST_CMD[@]}"
    )

    dotnet-trace "${TRACE_ARGS[@]}" 2>&1 | tee "$OUTPUT_DIR/trace.log"
    echo "  ✓ Trace saved: $TRACE_FILE"

    # Convert to requested format if not nettrace
    if [[ "$TRACE_FORMAT" != "nettrace" && -f "$TRACE_FILE" ]]; then
        echo "  Converting to $TRACE_FORMAT..."
        CONVERTED_FILE="$OUTPUT_DIR/trace.$TRACE_FORMAT"
        dotnet-trace convert "$TRACE_FILE" --format "$TRACE_FORMAT" --output "$CONVERTED_FILE" 2>/dev/null || true
        if [[ -f "$CONVERTED_FILE" ]]; then
            echo "  ✓ Converted: $CONVERTED_FILE"
        fi
    fi

    echo ""
else
    echo "▶ Skipping trace collection (--trace-profile none)"
    echo ""
fi

# ── Step 3: dotnet-counters ──────────────────────────────────────────────────

COUNTERS_FILE="$OUTPUT_DIR/counters.csv"
echo "▶ Collecting runtime counters (interval: ${COUNTERS_INTERVAL}s)..."

# Run the test exe in the background and attach counters
"${TEST_CMD[@]}" &
TEST_PID=$!

# Give the process a moment to start
sleep 1

if kill -0 "$TEST_PID" 2>/dev/null; then
    COUNTER_PROVIDERS="System.Runtime,Microsoft.AspNetCore.Hosting,Microsoft-Extensions-DependencyInjection"

    dotnet-counters collect \
        --process-id "$TEST_PID" \
        --output "$COUNTERS_FILE" \
        --format csv \
        --refresh-interval "$COUNTERS_INTERVAL" \
        --counters "$COUNTER_PROVIDERS" \
        2>&1 | tee "$OUTPUT_DIR/counters.log" &
    COUNTERS_PID=$!

    # Wait for the test process to finish
    wait "$TEST_PID" 2>/dev/null || true

    # Give counters a moment to flush, then stop
    sleep 2
    kill "$COUNTERS_PID" 2>/dev/null || true
    wait "$COUNTERS_PID" 2>/dev/null || true

    echo "  ✓ Counters saved: $COUNTERS_FILE"
else
    echo "  ⚠ Test process exited too quickly for counter collection"
    wait "$TEST_PID" 2>/dev/null || true
fi
echo ""

# ── Step 4: Memory dump (optional) ───────────────────────────────────────────

if [[ "$COLLECT_DUMP" == true ]]; then
    DUMP_FILE="$OUTPUT_DIR/dump.dmp"
    echo "▶ Collecting memory dump..."

    # Run test exe again, capture dump mid-execution
    "${TEST_CMD[@]}" &
    DUMP_PID=$!

    # Wait a bit for the process to get into steady state
    sleep 3

    if kill -0 "$DUMP_PID" 2>/dev/null; then
        dotnet-dump collect \
            --process-id "$DUMP_PID" \
            --output "$DUMP_FILE" \
            --type Full \
            2>&1 | tee "$OUTPUT_DIR/dump.log"
        echo "  ✓ Dump saved: $DUMP_FILE"

        # Let the test finish
        wait "$DUMP_PID" 2>/dev/null || true
    else
        echo "  ⚠ Test process exited before dump could be captured"
        echo "    Try using a filter that selects more/slower tests"
    fi
    echo ""
fi

# ── Summary ───────────────────────────────────────────────────────────────────

echo "═══════════════════════════════════════════════════════════════════"
echo "  Profiling complete! Output: $OUTPUT_DIR"
echo "═══════════════════════════════════════════════════════════════════"
echo ""
echo "  Files:"
for f in "$OUTPUT_DIR"/*; do
    if [[ -f "$f" ]]; then
        SIZE=$(du -h "$f" 2>/dev/null | cut -f1)
        echo "    $SIZE  $(basename "$f")"
    fi
done
echo ""
echo "  How to analyze:"
echo ""
echo "  Trace (.nettrace):"
echo "    - Visual Studio: File > Open > trace.nettrace"
echo "    - PerfView:      perfview.exe trace.nettrace"
echo "    - speedscope:    https://speedscope.app (open trace.speedscope)"
echo ""
echo "  Counters (.csv):"
echo "    - Excel/LibreOffice: Open counters.csv"
echo "    - Python: pandas.read_csv('counters.csv')"
echo ""
if [[ "$COLLECT_DUMP" == true ]]; then
    echo "  Dump (.dmp):"
    echo "    - Visual Studio: File > Open > dump.dmp"
    echo "    - dotnet-dump:   dotnet-dump analyze dump.dmp"
    echo "      > dumpheap -stat        (heap statistics)"
    echo "      > dumpheap -type <Type> (find specific types)"
    echo "      > gcroot <addr>         (find GC roots)"
    echo ""
fi
echo "═══════════════════════════════════════════════════════════════════"
