# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.860 μs   | 0.0305 μs | 0.0255 μs | 6.23 KB   |
| Imposter        | 2.694 μs   | 0.0510 μs | 0.0545 μs | 15.71 KB  |
| Mockolate       | 1.727 μs   | 0.0335 μs | 0.0314 μs | 7.36 KB   |
| Moq             | 413.347 μs | 3.2439 μs | 3.0343 μs | 36.46 KB  |
| NSubstitute     | 19.395 μs  | 0.1423 μs | 0.1262 μs | 26.72 KB  |
| FakeItEasy      | 18.531 μs  | 0.1090 μs | 0.1020 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
