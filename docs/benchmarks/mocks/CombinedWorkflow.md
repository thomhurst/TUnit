# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.845 μs   | 0.0172 μs | 0.0161 μs | 6.23 KB   |
| Imposter        | 2.595 μs   | 0.0218 μs | 0.0204 μs | 15.71 KB  |
| Mockolate       | 1.615 μs   | 0.0281 μs | 0.0249 μs | 7.36 KB   |
| Moq             | 412.817 μs | 1.4010 μs | 1.1699 μs | 36.49 KB  |
| NSubstitute     | 19.352 μs  | 0.2040 μs | 0.1908 μs | 26.72 KB  |
| FakeItEasy      | 19.245 μs  | 0.1490 μs | 0.1394 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-22T02:33:33.738Z*
