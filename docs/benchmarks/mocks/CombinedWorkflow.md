# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.451 μs   | 0.0070 μs | 0.0062 μs | 6.23 KB   |
| Imposter        | 1.977 μs   | 0.0175 μs | 0.0164 μs | 15.71 KB  |
| Mockolate       | 1.237 μs   | 0.0148 μs | 0.0138 μs | 7.36 KB   |
| Moq             | 240.011 μs | 1.3560 μs | 1.2021 μs | 36.49 KB  |
| NSubstitute     | 13.235 μs  | 0.0733 μs | 0.0686 μs | 26.72 KB  |
| FakeItEasy      | 12.310 μs  | 0.0680 μs | 0.0603 μs | 25.68 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-17T02:33:27.459Z*
