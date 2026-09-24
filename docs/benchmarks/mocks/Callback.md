# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean        | Error       | StdDev      | Allocated |
| --------------- | ----------- | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 713.3 ns    | 13.94 ns    | 14.32 ns    | 3.11 KB   |
| Imposter        | 451.7 ns    | 8.49 ns     | 7.09 ns     | 2.66 KB   |
| Mockolate       | 374.7 ns    | 7.19 ns     | 8.83 ns     | 1.8 KB    |
| Moq             | 79,049.6 ns | 1,368.82 ns | 1,213.42 ns | 13.24 KB  |
| NSubstitute     | 4,301.2 ns  | 74.72 ns    | 69.89 ns    | 7.85 KB   |
| FakeItEasy      | 3,776.8 ns  | 64.82 ns    | 60.63 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 827.0 ns    | 16.21 ns  | 21.08 ns  | 3.2 KB    |
| Imposter        | 523.2 ns    | 9.06 ns   | 8.03 ns   | 2.82 KB   |
| Mockolate       | 423.8 ns    | 8.39 ns   | 9.32 ns   | 1.84 KB   |
| Moq             | 82,506.9 ns | 562.36 ns | 439.06 ns | 13.71 KB  |
| NSubstitute     | 4,676.4 ns  | 35.03 ns  | 31.05 ns  | 8.41 KB   |
| FakeItEasy      | 4,793.4 ns  | 58.74 ns  | 54.94 ns  | 9.27 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-24T02:33:35.117Z*
