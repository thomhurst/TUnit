# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.496 μs   | 0.0187 μs | 0.0175 μs | 6.28 KB   |
| Imposter        | 1.959 μs   | 0.0190 μs | 0.0168 μs | 15.64 KB  |
| Mockolate       | 1.223 μs   | 0.0076 μs | 0.0063 μs | 7.36 KB   |
| Moq             | 237.140 μs | 1.1683 μs | 1.0928 μs | 36.16 KB  |
| NSubstitute     | 13.304 μs  | 0.1067 μs | 0.0946 μs | 26.72 KB  |
| FakeItEasy      | 12.043 μs  | 0.0330 μs | 0.0276 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-08T02:41:51.713Z*
