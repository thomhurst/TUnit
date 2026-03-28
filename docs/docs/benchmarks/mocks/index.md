---
title: Mock Library Benchmarks
description: Performance comparisons between TUnit.Mocks, Moq, NSubstitute, and FakeItEasy
sidebar_position: 1
---

# Mock Library Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **2026-03-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
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

Click on any benchmark to view detailed results:

- [Callback](Callback) - Callback registration and execution
- [CombinedWorkflow](CombinedWorkflow) - Full workflow: create → setup → invoke → verify
- [Invocation](Invocation) - Calling methods on mock objects
- [MockCreation](MockCreation) - Mock instance creation performance
- [Setup](Setup) - Mock behavior configuration (returns, matchers)
- [Verification](Verification) - Verifying mock method calls

## 📈 What's Measured

Each benchmark category tests a specific aspect of mocking library usage:

- **MockCreation** — How fast can each library create a mock instance?
- **Setup** — How fast can you configure return values and argument matchers?
- **Invocation** — Once set up, how fast are method calls on the mock?
- **Verification** — How fast can you verify that methods were called correctly?
- **Callback** — How fast are callbacks triggered during mock invocations?
- **CombinedWorkflow** — The full real-world pattern: create → setup → invoke → verify

## 🔧 Methodology

- **Tool**: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
- **OS**: Ubuntu Latest (GitHub Actions)
- **Runtime**: .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
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

*Last generated: 2026-03-28T22:34:52.304Z*
