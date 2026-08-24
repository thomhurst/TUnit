# Mock Library Benchmarks

Last Updated

These benchmarks were automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 🚀 Overview[​](#-overview "Direct link to 🚀 Overview")

These benchmarks compare source-generated, AOT-compatible mocking libraries against the most popular .NET mocking libraries that use runtime proxy generation:

| Library         | Approach                              | AOT Compatible |
| --------------- | ------------------------------------- | -------------- |
| **TUnit.Mocks** | Source-generated at compile time      | ✅ Yes         |
| **Imposter**    | Source-generated at compile time      | ✅ Yes         |
| **Mockolate**   | Source-generated at compile time      | ✅ Yes         |
| **Moq**         | Runtime proxy via Castle.DynamicProxy | ❌ No          |
| **NSubstitute** | Runtime proxy via Castle.DynamicProxy | ❌ No          |
| **FakeItEasy**  | Runtime proxy via Castle.DynamicProxy | ❌ No          |

## 📊 Benchmark Categories[​](#-benchmark-categories "Direct link to 📊 Benchmark Categories")

Click on any benchmark to view detailed results:

* [Callback](/docs/benchmarks/mocks/Callback.md) - Callback registration and execution
* [CombinedWorkflow](/docs/benchmarks/mocks/CombinedWorkflow.md) - Full workflow: create → setup → invoke → verify
* [Invocation](/docs/benchmarks/mocks/Invocation.md) - Calling methods on mock objects
* [MockCreation](/docs/benchmarks/mocks/MockCreation.md) - Mock instance creation performance
* [Setup](/docs/benchmarks/mocks/Setup.md) - Mock behavior configuration (returns, matchers)
* [Verification](/docs/benchmarks/mocks/Verification.md) - Verifying mock method calls

## 📈 What's Measured[​](#-whats-measured "Direct link to 📈 What's Measured")

Each benchmark category tests a specific aspect of mocking library usage:

* **MockCreation** — Mock instance creation performance
* **Setup** — Mock behavior configuration (returns, matchers)
* **Invocation** — Calling methods on mock objects
* **Verification** — Verifying mock method calls
* **Callback** — Callback registration and execution
* **CombinedWorkflow** — Full workflow: create → setup → invoke → verify

## 🔧 Methodology[​](#-methodology "Direct link to 🔧 Methodology")

* **Tool**: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
* **OS**: Ubuntu Latest (GitHub Actions)
* **Runtime**: .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
* **Statistical Rigor**: Multiple iterations with warm-up and outlier detection
* **Memory**: Allocation tracking enabled via `[MemoryDiagnoser]`

### Why Source-Generated Mocks?[​](#why-source-generated-mocks "Direct link to Why Source-Generated Mocks?")

TUnit.Mocks generates mock implementations at compile time, eliminating:

* Runtime proxy generation overhead
* Dynamic assembly emission
* Reflection-based method dispatch

This makes TUnit.Mocks compatible with **Native AOT** and **IL trimming**, while also providing performance benefits for standard .NET execution.

### Source Code[​](#source-code "Direct link to Source Code")

All benchmark source code is available in the [`TUnit.Mocks.Benchmarks`](https://github.com/thomhurst/TUnit/tree/main/benchmarks/TUnit.Mocks.Benchmarks) directory.

***

Continuous Benchmarking

These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/mock-benchmarks.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.

*Last generated: 2026-08-24T02:46:06.016Z*
