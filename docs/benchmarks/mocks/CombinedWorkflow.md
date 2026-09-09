# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.468 μs   | 0.0048 μs | 0.0040 μs | 6.23 KB   |
| Imposter        | 1.942 μs   | 0.0096 μs | 0.0085 μs | 15.71 KB  |
| Mockolate       | 1.254 μs   | 0.0050 μs | 0.0047 μs | 7.36 KB   |
| Moq             | 161.910 μs | 2.6025 μs | 2.4344 μs | 36.08 KB  |
| NSubstitute     | 14.622 μs  | 0.1353 μs | 0.1130 μs | 26.72 KB  |
| FakeItEasy      | 10.942 μs  | 0.0615 μs | 0.0513 μs | 25.51 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-09T02:32:56.707Z*
