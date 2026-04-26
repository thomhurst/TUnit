---
title: Mock Library Benchmarks
description: Performance comparisons between TUnit.Mocks, Imposter, Mockolate, Moq, NSubstitute, FakeItEasy
sidebar_position: 1
---

# Mock Library Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **2026-04-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 🚀 Overview

These benchmarks compare source-generated, AOT-compatible mocking libraries against the most popular .NET mocking libraries that use runtime proxy generation:

| Library | Approach | AOT Compatible |
|---------|----------|----------------|
| **TUnit.Mocks** | Source-generated at compile time | ✅ Yes |
| **Imposter** | Source-generated at compile time | ✅ Yes |
| **Mockolate** | Source-generated at compile time | ✅ Yes |
| **Moq** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **NSubstitute** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **FakeItEasy** | Runtime proxy via Castle.DynamicProxy | ❌ No |

## 📊 Benchmark Categories

Click on any benchmark to view detailed results:

- [Callback](./Callback.md) - Callback registration and execution
- [CombinedWorkflow](./CombinedWorkflow.md) - Full workflow: create → setup → invoke → verify
- [Invocation](./Invocation.md) - Calling methods on mock objects
- [MockCreation](./MockCreation.md) - Mock instance creation performance
- [Setup](./Setup.md) - Mock behavior configuration (returns, matchers)
- [Verification](./Verification.md) - Verifying mock method calls

## 📈 What's Measured

Each benchmark category tests a specific aspect of mocking library usage:

- **MockCreation** — Mock instance creation performance
- **Setup** — Mock behavior configuration (returns, matchers)
- **Invocation** — Calling methods on mock objects
- **Verification** — Verifying mock method calls
- **Callback** — Callback registration and execution
- **CombinedWorkflow** — Full workflow: create → setup → invoke → verify

## 🔧 Methodology

- **Tool**: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
- **OS**: Ubuntu Latest (GitHub Actions)
- **Runtime**: .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
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

*Last generated: 2026-04-26T03:29:14.435Z*
