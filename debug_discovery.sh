#!/bin/bash

echo "=== TUnit Discovery Debug Script ==="
echo

# Enable all diagnostics
export TUNIT_DISCOVERY_DIAGNOSTICS=1
export TUNIT_DISCOVERY_TIMEOUT_SECONDS=10  # Reduce timeout for faster debugging
export TUNIT_MAX_COMBINATIONS=100          # Limit combinations
export TUNIT_MAX_DATA_ITEMS=50            # Limit data items

echo "Configuration:"
echo "- Discovery timeout: 10 seconds"
echo "- Max combinations: 100"
echo "- Max data items: 50"
echo "- Diagnostics: ENABLED"
echo

# Run a simple test project to see if discovery works
echo "Running discovery on a test project..."
cd TUnit.TestProject 2>/dev/null || cd */TUnit.TestProject 2>/dev/null || echo "Could not find TUnit.TestProject"

# Try to run with verbose output
echo "Attempting test discovery..."
timeout 15 dotnet run --list-tests 2>&1 | tee discovery_output.log

echo
echo "=== Discovery Output Analysis ==="

# Check for common errors
if grep -q "TestRegistry has not been initialized" discovery_output.log 2>/dev/null; then
    echo "❌ FOUND: TestRegistry initialization error"
    echo "   This is the most common cause of hanging"
    echo "   The framework needs to initialize TestRegistry before discovery"
fi

if grep -q "Cartesian product exceeded" discovery_output.log 2>/dev/null; then
    echo "❌ FOUND: Excessive cartesian product expansion"
    echo "   Reduce the size of your test data sources"
fi

if grep -q "timed out after" discovery_output.log 2>/dev/null; then
    echo "❌ FOUND: Discovery timeout"
    echo "   Check the full output above for the root cause"
fi

echo
echo "Check discovery_output.log for full details"