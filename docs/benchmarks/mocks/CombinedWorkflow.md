# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.896 μs   | 0.0234 μs | 0.0219 μs | 6.23 KB   |
| Imposter        | 2.618 μs   | 0.0389 μs | 0.0364 μs | 15.71 KB  |
| Mockolate       | 1.638 μs   | 0.0290 μs | 0.0257 μs | 7.36 KB   |
| Moq             | 307.866 μs | 3.6646 μs | 3.2486 μs | 36.46 KB  |
| NSubstitute     | 17.246 μs  | 0.3134 μs | 0.2931 μs | 26.72 KB  |
| FakeItEasy      | 15.431 μs  | 0.1312 μs | 0.1163 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-03T02:45:05.205Z*
