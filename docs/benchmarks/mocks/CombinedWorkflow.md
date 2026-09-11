# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.467 μs   | 0.0080 μs | 0.0075 μs | 6.23 KB   |
| Imposter        | 2.067 μs   | 0.0411 μs | 0.0675 μs | 15.71 KB  |
| Mockolate       | 1.277 μs   | 0.0219 μs | 0.0205 μs | 7.36 KB   |
| Moq             | 241.052 μs | 0.7169 μs | 0.6355 μs | 36.16 KB  |
| NSubstitute     | 13.451 μs  | 0.0860 μs | 0.0762 μs | 26.72 KB  |
| FakeItEasy      | 12.434 μs  | 0.1627 μs | 0.1522 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-11T02:38:48.126Z*
