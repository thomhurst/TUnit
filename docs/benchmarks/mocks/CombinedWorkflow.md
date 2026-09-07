# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.810 μs   | 0.0132 μs | 0.0117 μs | 6.23 KB   |
| Imposter        | 2.670 μs   | 0.0524 μs | 0.0846 μs | 15.71 KB  |
| Mockolate       | 1.828 μs   | 0.0331 μs | 0.0354 μs | 7.36 KB   |
| Moq             | 412.277 μs | 3.0604 μs | 2.7130 μs | 36.55 KB  |
| NSubstitute     | 19.485 μs  | 0.2501 μs | 0.2340 μs | 26.89 KB  |
| FakeItEasy      | 17.841 μs  | 0.3499 μs | 0.5129 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-07T02:34:20.667Z*
