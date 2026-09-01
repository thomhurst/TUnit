# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.981 μs   | 0.0153 μs | 0.0143 μs | 6.23 KB   |
| Imposter        | 2.984 μs   | 0.0430 μs | 0.0402 μs | 15.71 KB  |
| Mockolate       | 1.729 μs   | 0.0086 μs | 0.0080 μs | 7.36 KB   |
| Moq             | 191.985 μs | 1.1553 μs | 1.0807 μs | 36.39 KB  |
| NSubstitute     | 18.627 μs  | 0.1397 μs | 0.1306 μs | 26.72 KB  |
| FakeItEasy      | 14.660 μs  | 0.0653 μs | 0.0611 μs | 25.59 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-01T02:34:33.391Z*
