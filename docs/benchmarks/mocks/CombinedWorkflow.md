# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 1,859.0 ns   | 36.27 ns    | 50.85 ns    | 6.31 KB   |
| Imposter        | 915.4 ns     | 18.22 ns    | 24.33 ns    | 3.45 KB   |
| Mockolate       | 1,142.1 ns   | 22.63 ns    | 28.62 ns    | 3.9 KB    |
| Moq             | 446,884.9 ns | 3,592.10 ns | 3,360.05 ns | 36.66 KB  |
| NSubstitute     | 18,978.9 ns  | 360.62 ns   | 354.18 ns   | 26.51 KB  |
| FakeItEasy      | 17,767.5 ns  | 238.30 ns   | 222.91 ns   | 24.96 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-18T02:25:26.946Z*
