# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.798 μs   | 0.0151 μs | 0.0141 μs | 6.23 KB   |
| Imposter        | 2.608 μs   | 0.0512 μs | 0.0911 μs | 15.71 KB  |
| Mockolate       | 1.601 μs   | 0.0167 μs | 0.0130 μs | 7.36 KB   |
| Moq             | 406.084 μs | 4.4923 μs | 4.2021 μs | 36.57 KB  |
| NSubstitute     | 19.410 μs  | 0.0842 μs | 0.0746 μs | 26.72 KB  |
| FakeItEasy      | 17.953 μs  | 0.3380 μs | 0.3162 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-25T02:32:28.119Z*
