# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.019 μs   | 0.0130 μs | 0.0115 μs | 6.28 KB   |
| Imposter        | 2.975 μs   | 0.0153 μs | 0.0143 μs | 15.64 KB  |
| Mockolate       | 1.764 μs   | 0.0105 μs | 0.0087 μs | 7.36 KB   |
| Moq             | 420.429 μs | 1.5157 μs | 1.3436 μs | 36.16 KB  |
| NSubstitute     | 19.471 μs  | 0.2452 μs | 0.2174 μs | 26.72 KB  |
| FakeItEasy      | 19.149 μs  | 0.0436 μs | 0.0387 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-10T02:40:36.038Z*
