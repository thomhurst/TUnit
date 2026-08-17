# Performance Benchmarks

Last Updated

These benchmarks were automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 🚀 Runtime Benchmarks[​](#-runtime-benchmarks "Direct link to 🚀 Runtime Benchmarks")

Click on any benchmark to view detailed results:

* [AsyncTests](/docs/benchmarks/engine/AsyncTests.md) — Realistic async/await patterns with I/O simulation
* [DataDrivenTests](/docs/benchmarks/engine/DataDrivenTests.md) — Parameterized tests with multiple data sources
* [MassiveParallelTests](/docs/benchmarks/engine/MassiveParallelTests.md) — Parallel execution stress tests
* [MatrixTests](/docs/benchmarks/engine/MatrixTests.md) — Combinatorial test generation and execution
* [ScaleTests](/docs/benchmarks/engine/ScaleTests.md) — Large test suites (150+ tests) measuring scalability
* [SetupTeardownTests](/docs/benchmarks/engine/SetupTeardownTests.md) — Expensive test fixtures with setup/teardown overhead

## 🔨 Build Benchmarks[​](#-build-benchmarks "Direct link to 🔨 Build Benchmarks")

* [Build Performance](/docs/benchmarks/engine/BuildTime.md) - Compilation time comparison

***

## 📊 Methodology[​](#-methodology "Direct link to 📊 Methodology")

These benchmarks compare TUnit against the most popular .NET testing frameworks:

| Framework    | Version Tested |
| ------------ | -------------- |
| **TUnit**    | 1.65.0         |
| **xUnit v3** | 4.0.0          |
| **NUnit**    | 4.6.1          |
| **MSTest**   | 4.3.3          |

### Test Scenarios[​](#test-scenarios "Direct link to Test Scenarios")

The benchmarks measure real-world testing patterns:

* **DataDrivenTests**: Parameterized tests with multiple data sources
* **AsyncTests**: Realistic async/await patterns with I/O simulation
* **ScaleTests**: Large test suites (150+ tests) measuring scalability
* **MatrixTests**: Combinatorial test generation and execution
* **MassiveParallelTests**: Parallel execution stress tests
* **SetupTeardownTests**: Expensive test fixtures with setup/teardown overhead

### Environment[​](#environment "Direct link to Environment")

* **OS**: Ubuntu Latest (GitHub Actions)
* **Runtime**: .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
* **SDK**: .NET SDK 10.0.400
* **Hardware**: GitHub Actions Standard Runner (Ubuntu)
* **Tool**: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)

### Why These Numbers Matter[​](#why-these-numbers-matter "Direct link to Why These Numbers Matter")

* **No Mocking**: All tests use realistic patterns, not artificial micro-benchmarks
* **Equivalent Logic**: Each framework implements identical test scenarios
* **Warm-Up Excluded**: Measurements exclude JIT warm-up overhead
* **Statistical Rigor**: Multiple iterations with outlier detection

### Source Code[​](#source-code "Direct link to Source Code")

All benchmark source code is available in the [`tools/speed-comparison`](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison) directory.

***

Continuous Benchmarking

These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.

*Last generated: 2026-08-17T16:25:00.426Z*
