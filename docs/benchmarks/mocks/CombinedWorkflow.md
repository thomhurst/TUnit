# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.878 μs   | 0.0122 μs | 0.0102 μs | 6.23 KB   |
| Imposter        | 2.719 μs   | 0.0225 μs | 0.0199 μs | 15.71 KB  |
| Mockolate       | 1.664 μs   | 0.0090 μs | 0.0070 μs | 7.36 KB   |
| Moq             | 407.883 μs | 2.2183 μs | 2.0750 μs | 36.35 KB  |
| NSubstitute     | 18.794 μs  | 0.0531 μs | 0.0471 μs | 26.72 KB  |
| FakeItEasy      | 18.191 μs  | 0.3525 μs | 0.3462 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-21T02:37:24.392Z*
