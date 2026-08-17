# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.011 μs   | 0.0209 μs | 0.0174 μs | 6.23 KB   |
| Imposter        | 2.608 μs   | 0.0378 μs | 0.0335 μs | 15.71 KB  |
| Mockolate       | 1.660 μs   | 0.0329 μs | 0.0451 μs | 7.36 KB   |
| Moq             | 305.261 μs | 1.0978 μs | 0.9167 μs | 36.35 KB  |
| NSubstitute     | 17.793 μs  | 0.1791 μs | 0.1675 μs | 26.72 KB  |
| FakeItEasy      | 15.855 μs  | 0.1515 μs | 0.1417 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T02:43:20.076Z*
