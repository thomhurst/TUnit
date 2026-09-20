# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.281 μs   | 0.0114 μs | 0.0095 μs | 6.23 KB   |
| Imposter        | 1.859 μs   | 0.0365 μs | 0.0620 μs | 15.71 KB  |
| Mockolate       | 1.110 μs   | 0.0219 μs | 0.0268 μs | 7.36 KB   |
| Moq             | 135.982 μs | 0.3264 μs | 0.2893 μs | 36.19 KB  |
| NSubstitute     | 12.806 μs  | 0.1449 μs | 0.1356 μs | 26.72 KB  |
| FakeItEasy      | 10.095 μs  | 0.1164 μs | 0.1089 μs | 25.67 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-20T02:32:50.293Z*
