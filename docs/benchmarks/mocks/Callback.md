# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean        | Error       | StdDev      | Allocated |
| --------------- | ----------- | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 616.1 ns    | 9.65 ns     | 9.03 ns     | 3.12 KB   |
| Imposter        | 402.3 ns    | 4.41 ns     | 3.68 ns     | 2.66 KB   |
| Mockolate       | 313.2 ns    | 4.34 ns     | 3.63 ns     | 1.8 KB    |
| Moq             | 80,614.9 ns | 1,363.16 ns | 1,275.10 ns | 13.24 KB  |
| NSubstitute     | 4,005.9 ns  | 79.25 ns    | 88.08 ns    | 7.85 KB   |
| FakeItEasy      | 3,283.2 ns  | 63.65 ns    | 95.27 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean        | Error       | StdDev      | Allocated |
| --------------- | ----------- | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 707.6 ns    | 7.56 ns     | 7.07 ns     | 3.21 KB   |
| Imposter        | 448.6 ns    | 1.86 ns     | 1.55 ns     | 2.82 KB   |
| Mockolate       | 357.7 ns    | 1.90 ns     | 1.68 ns     | 1.84 KB   |
| Moq             | 82,614.6 ns | 1,608.07 ns | 1,651.37 ns | 13.67 KB  |
| NSubstitute     | 4,576.0 ns  | 90.79 ns    | 253.08 ns   | 8.41 KB   |
| FakeItEasy      | 4,779.7 ns  | 92.08 ns    | 132.06 ns   | 9.41 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-06T02:37:20.591Z*
