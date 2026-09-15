# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.911 μs   | 0.0234 μs | 0.0219 μs | 6.23 KB   |
| Imposter        | 2.623 μs   | 0.0392 μs | 0.0347 μs | 15.71 KB  |
| Mockolate       | 1.628 μs   | 0.0228 μs | 0.0213 μs | 7.36 KB   |
| Moq             | 186.717 μs | 0.9995 μs | 0.8346 μs | 36.27 KB  |
| NSubstitute     | 18.592 μs  | 0.1087 μs | 0.1017 μs | 26.72 KB  |
| FakeItEasy      | 13.788 μs  | 0.0785 μs | 0.0696 μs | 25.51 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-15T02:33:23.208Z*
