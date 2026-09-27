# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.063 μs   | 0.0130 μs | 0.0122 μs | 6.23 KB   |
| Imposter        | 3.163 μs   | 0.0509 μs | 0.0476 μs | 15.71 KB  |
| Mockolate       | 1.871 μs   | 0.0229 μs | 0.0191 μs | 7.36 KB   |
| Moq             | 409.770 μs | 1.3919 μs | 1.2339 μs | 36.3 KB   |
| NSubstitute     | 19.723 μs  | 0.1285 μs | 0.1073 μs | 26.72 KB  |
| FakeItEasy      | 19.149 μs  | 0.0606 μs | 0.0567 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-27T02:37:02.890Z*
