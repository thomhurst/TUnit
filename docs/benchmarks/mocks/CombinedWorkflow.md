# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.879 μs   | 0.0215 μs | 0.0202 μs | 6.28 KB   |
| Imposter        | 2.505 μs   | 0.0328 μs | 0.0290 μs | 15.71 KB  |
| Mockolate       | 1.688 μs   | 0.0337 μs | 0.0572 μs | 7.36 KB   |
| Moq             | 314.216 μs | 1.8407 μs | 1.5370 μs | 36.28 KB  |
| NSubstitute     | 17.645 μs  | 0.1829 μs | 0.1621 μs | 26.72 KB  |
| FakeItEasy      | 15.404 μs  | 0.1215 μs | 0.1077 μs | 25.67 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T03:10:54.658Z*
