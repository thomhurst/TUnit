# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.089 μs   | 0.0310 μs | 0.0332 μs | 6.28 KB   |
| Imposter        | 2.931 μs   | 0.0587 μs | 0.1102 μs | 15.64 KB  |
| Mockolate       | 1.758 μs   | 0.0319 μs | 0.0299 μs | 7.36 KB   |
| Moq             | 319.354 μs | 1.7160 μs | 1.6052 μs | 36.25 KB  |
| NSubstitute     | 18.805 μs  | 0.2267 μs | 0.2010 μs | 26.72 KB  |
| FakeItEasy      | 17.885 μs  | 0.2680 μs | 0.2376 μs | 25.5 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-07T02:41:59.908Z*
