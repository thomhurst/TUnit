# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.857 μs   | 0.0199 μs | 0.0186 μs | 6.31 KB   |
| Imposter        | 2.368 μs   | 0.0183 μs | 0.0162 μs | 14.95 KB  |
| Mockolate       | 1.623 μs   | 0.0305 μs | 0.0285 μs | 7.36 KB   |
| Moq             | 410.970 μs | 2.3443 μs | 2.1929 μs | 36.49 KB  |
| NSubstitute     | 19.293 μs  | 0.1824 μs | 0.1706 μs | 26.72 KB  |
| FakeItEasy      | 19.421 μs  | 0.0818 μs | 0.0766 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-25T02:33:24.276Z*
