# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.908 μs   | 0.0306 μs | 0.0255 μs | 6.28 KB   |
| Imposter        | 2.741 μs   | 0.0523 μs | 0.0602 μs | 15.71 KB  |
| Mockolate       | 1.718 μs   | 0.0234 μs | 0.0208 μs | 7.36 KB   |
| Moq             | 410.115 μs | 3.3832 μs | 3.1646 μs | 36.35 KB  |
| NSubstitute     | 19.715 μs  | 0.0440 μs | 0.0390 μs | 26.72 KB  |
| FakeItEasy      | 19.303 μs  | 0.1926 μs | 0.1707 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-02T02:44:13.542Z*
