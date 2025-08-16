# TUnit Speed Comparison Benchmarks

This directory contains realistic benchmarks comparing TUnit, xUnit, NUnit, and MSTest frameworks.

## Test Categories

Each framework implements the following test categories:

1. **BasicTests** - Simple test methods with assertions and basic operations
2. **DataDrivenTests** - Parameterized tests using framework-specific data sources
3. **SetupTeardownTests** - Tests with lifecycle hooks (setup/teardown)
4. **AsyncTests** - Realistic async patterns using in-memory operations
5. **FixtureTests** - Class-level fixtures and resource management
6. **ParallelTests** - Tests demonstrating parallel execution patterns
7. **AssertionTests** - Various assertion patterns and complex validations
8. **RepeatTests** - Repeated test execution scenarios

## Running Benchmarks

### Build all projects first:
```bash
dotnet build -c Release
```

### Run specific benchmark categories:
```bash
# Build benchmarks (measures compilation time)
dotnet run -c Release --project Tests.Benchmark -- --filter "*BuildBenchmarks*"

# Runtime benchmarks for specific test class
set CLASS_NAME=BasicTests
dotnet run -c Release --project Tests.Benchmark -- --filter "*RuntimeBenchmarks*"

# Available test class names:
# - BasicTests
# - DataDrivenTests  
# - SetupTeardownTests
# - AsyncTests
# - FixtureTests
# - ParallelTests
# - AssertionTests
# - RepeatTests
```

### Run all benchmarks:
```bash
dotnet run -c Release --project Tests.Benchmark
```

## Benchmark Details

- TUnit runs in both AOT (Ahead-of-Time) and regular JIT modes
- All frameworks implement equivalent test logic
- Tests use realistic patterns without external dependencies
- In-memory operations simulate I/O without file system artifacts

## Results

Benchmark results are output as Markdown files in the BenchmarkDotNet.Artifacts directory.