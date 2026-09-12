# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.789 μs   | 0.0093 μs | 0.0077 μs | 6.23 KB   |
| Imposter        | 2.537 μs   | 0.0168 μs | 0.0140 μs | 15.71 KB  |
| Mockolate       | 1.583 μs   | 0.0056 μs | 0.0053 μs | 7.36 KB   |
| Moq             | 407.520 μs | 2.3144 μs | 2.0516 μs | 36.35 KB  |
| NSubstitute     | 18.894 μs  | 0.1575 μs | 0.1315 μs | 26.72 KB  |
| FakeItEasy      | 17.927 μs  | 0.0863 μs | 0.0674 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-12T02:33:29.738Z*
