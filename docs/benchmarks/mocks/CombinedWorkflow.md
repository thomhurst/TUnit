# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.802 μs   | 0.0065 μs | 0.0054 μs | 6.23 KB   |
| Imposter        | 2.627 μs   | 0.0198 μs | 0.0176 μs | 15.71 KB  |
| Mockolate       | 1.625 μs   | 0.0078 μs | 0.0069 μs | 7.36 KB   |
| Moq             | 407.967 μs | 1.8033 μs | 1.5986 μs | 36.68 KB  |
| NSubstitute     | 18.820 μs  | 0.0947 μs | 0.0885 μs | 26.89 KB  |
| FakeItEasy      | 18.189 μs  | 0.0842 μs | 0.0788 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-24T02:33:35.117Z*
