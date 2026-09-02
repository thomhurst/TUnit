# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.884 μs   | 0.0190 μs | 0.0178 μs | 6.23 KB   |
| Imposter        | 2.601 μs   | 0.0511 μs | 0.0683 μs | 15.71 KB  |
| Mockolate       | 1.659 μs   | 0.0326 μs | 0.0305 μs | 7.36 KB   |
| Moq             | 313.752 μs | 3.5154 μs | 3.2883 μs | 36.72 KB  |
| NSubstitute     | 17.693 μs  | 0.1651 μs | 0.1378 μs | 26.72 KB  |
| FakeItEasy      | 15.517 μs  | 0.1922 μs | 0.1605 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-02T02:49:53.672Z*
