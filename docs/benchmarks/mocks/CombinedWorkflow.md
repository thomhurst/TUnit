# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.221 μs   | 0.0031 μs | 0.0027 μs | 6.23 KB   |
| Imposter        | 1.711 μs   | 0.0064 μs | 0.0056 μs | 15.71 KB  |
| Mockolate       | 1.045 μs   | 0.0057 μs | 0.0054 μs | 7.36 KB   |
| Moq             | 137.647 μs | 0.7166 μs | 0.5984 μs | 36.09 KB  |
| NSubstitute     | 12.273 μs  | 0.1590 μs | 0.2428 μs | 26.72 KB  |
| FakeItEasy      | 9.255 μs   | 0.0389 μs | 0.0344 μs | 25.66 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-18T02:32:22.133Z*
