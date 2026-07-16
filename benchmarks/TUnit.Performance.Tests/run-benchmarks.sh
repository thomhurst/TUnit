#!/bin/bash

# TUnit Performance Benchmark Runner
# Usage: ./run-benchmarks.sh [options]
# Options:
#   -f, --filter <pattern>    Filter benchmarks (e.g., "*Discovery*")
#   -j, --job <job>          Specify runtime (Net80, NativeAot80, or All)
#   -q, --quick              Run quick benchmarks with reduced iterations
#   -h, --help               Show help

set -e

FILTER="*"
JOB="All"
QUICK=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        -j|--job)
            JOB="$2"
            shift 2
            ;;
        -q|--quick)
            QUICK=true
            shift
            ;;
        -h|--help)
            echo "TUnit Performance Benchmark Runner"
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -f, --filter <pattern>    Filter benchmarks (e.g., '*Discovery*')"
            echo "  -j, --job <job>          Specify runtime (Net80, NativeAot80, or All)"
            echo "  -q, --quick              Run quick benchmarks with reduced iterations"
            echo "  -h, --help               Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "\033[32mBuilding TUnit Performance Tests...\033[0m"
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo -e "\033[31mBuild failed\033[0m"
    exit 1
fi

ARGS=()

if [ "$FILTER" != "*" ]; then
    ARGS+=("--filter" "$FILTER")
fi

if [ "$JOB" = "Net80" ]; then
    ARGS+=("--job" "Net80")
elif [ "$JOB" = "NativeAot80" ]; then
    ARGS+=("--job" "NativeAot80")
fi

if [ "$QUICK" = true ]; then
    ARGS+=("--iterationCount" "3" "--warmupCount" "1")
fi

echo -e "\033[36mRunning benchmarks with arguments: ${ARGS[*]}\033[0m"
dotnet run -c Release -- "${ARGS[@]}"

echo -e "\n\033[33mBenchmark results are saved in BenchmarkDotNet.Artifacts/results/\033[0m"