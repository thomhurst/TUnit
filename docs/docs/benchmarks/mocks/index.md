---
title: Mock Library Benchmarks
description: Performance comparisons between TUnit.Mocks, Moq, NSubstitute, and FakeItEasy
sidebar_position: 1
---

# Mock Library Benchmarks

:::info Awaiting First Run
These benchmarks will be automatically populated after the first [Mock Benchmarks workflow](https://github.com/thomhurst/TUnit/actions/workflows/mock-benchmarks.yml) run completes.
:::

## 🚀 Overview

These benchmarks compare **TUnit.Mocks** (source-generated, AOT-compatible) against the most popular .NET mocking libraries that use runtime proxy generation:

| Library | Approach | AOT Compatible |
|---------|----------|----------------|
| **TUnit.Mocks** | Source-generated at compile time | ✅ Yes |
| **Moq** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **NSubstitute** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **FakeItEasy** | Runtime proxy via Castle.DynamicProxy | ❌ No |

## 📊 Benchmark Categories

- [Mock Creation](MockCreation) - Mock instance creation performance
- [Setup](Setup) - Mock behavior configuration (returns, matchers)
- [Invocation](Invocation) - Calling methods on mock objects
- [Verification](Verification) - Verifying mock method calls
- [Callback](Callback) - Callback registration and execution
- [Combined Workflow](CombinedWorkflow) - Full workflow: create → setup → invoke → verify

## 📈 What's Measured

Each benchmark category tests a specific aspect of mocking library usage:

- **MockCreation** — How fast can each library create a mock instance?
- **Setup** — How fast can you configure return values and argument matchers?
- **Invocation** — Once set up, how fast are method calls on the mock?
- **Verification** — How fast can you verify that methods were called correctly?
- **Callback** — How fast are callbacks triggered during mock invocations?
- **CombinedWorkflow** — The full real-world pattern: create → setup → invoke → verify

## 🔧 Methodology

- **Tool**: BenchmarkDotNet
- **OS**: Ubuntu Latest (GitHub Actions)
- **Statistical Rigor**: Multiple iterations with warm-up and outlier detection
- **Memory**: Allocation tracking enabled via `[MemoryDiagnoser]`

### Why Source-Generated Mocks?

TUnit.Mocks generates mock implementations at compile time, eliminating:
- Runtime proxy generation overhead
- Dynamic assembly emission
- Reflection-based method dispatch

This makes TUnit.Mocks compatible with **Native AOT** and **IL trimming**, while also providing performance benefits for standard .NET execution.

### Source Code

All benchmark source code is available in the [`TUnit.Mocks.Benchmarks`](https://github.com/thomhurst/TUnit/tree/main/TUnit.Mocks.Benchmarks) directory.

---

:::note Continuous Benchmarking
These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/mock-benchmarks.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.
:::
