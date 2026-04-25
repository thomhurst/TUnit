---
title: Performance Benchmarks
description: Real-world performance comparisons between TUnit and other .NET testing frameworks
sidebar_position: 1
---

# Performance Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **2026-04-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 🚀 Runtime Benchmarks

Click on any benchmark to view detailed results:

- [AsyncTests](./AsyncTests.md) — Realistic async/await patterns with I/O simulation
- [DataDrivenTests](./DataDrivenTests.md) — Parameterized tests with multiple data sources
- [MassiveParallelTests](./MassiveParallelTests.md) — Parallel execution stress tests
- [MatrixTests](./MatrixTests.md) — Combinatorial test generation and execution
- [ScaleTests](./ScaleTests.md) — Large test suites (150+ tests) measuring scalability
- [SetupTeardownTests](./SetupTeardownTests.md) — Expensive test fixtures with setup/teardown overhead


## 🔨 Build Benchmarks

- [Build Performance](./BuildTime.md) - Compilation time comparison


---

## 📊 Methodology

These benchmarks compare TUnit against the most popular .NET testing frameworks:

| Framework | Version Tested |
|-----------|----------------|
| **TUnit** | 1.39.0 |
| **xUnit v3** | 3.2.2 |
| **NUnit** | 4.5.1 |
| **MSTest** | 4.2.1 |

### Test Scenarios

The benchmarks measure real-world testing patterns:

- **DataDrivenTests**: Parameterized tests with multiple data sources
- **AsyncTests**: Realistic async/await patterns with I/O simulation
- **ScaleTests**: Large test suites (150+ tests) measuring scalability
- **MatrixTests**: Combinatorial test generation and execution
- **MassiveParallelTests**: Parallel execution stress tests
- **SetupTeardownTests**: Expensive test fixtures with setup/teardown overhead

### Environment

- **OS**: Ubuntu Latest (GitHub Actions)
- **Runtime**: .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
- **SDK**: .NET SDK 10.0.203
- **Hardware**: GitHub Actions Standard Runner (Ubuntu)
- **Tool**: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)

### Why These Numbers Matter

- **No Mocking**: All tests use realistic patterns, not artificial micro-benchmarks
- **Equivalent Logic**: Each framework implements identical test scenarios
- **Warm-Up Excluded**: Measurements exclude JIT warm-up overhead
- **Statistical Rigor**: Multiple iterations with outlier detection

### Source Code

All benchmark source code is available in the [`tools/speed-comparison`](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison) directory.

---

:::note Continuous Benchmarking
These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.
:::

*Last generated: 2026-04-25T00:43:27.067Z*
