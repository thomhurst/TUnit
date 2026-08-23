# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 741.5 ns     | 14.65 ns    | 20.54 ns  | 3.11 KB   |
| Imposter        | 502.3 ns     | 9.68 ns     | 11.89 ns  | 2.66 KB   |
| Mockolate       | 379.0 ns     | 7.60 ns     | 7.11 ns   | 1.8 KB    |
| Moq             | 189,971.1 ns | 1,067.16 ns | 946.01 ns | 13.26 KB  |
| NSubstitute     | 5,249.0 ns   | 55.34 ns    | 51.76 ns  | 7.85 KB   |
| FakeItEasy      | 5,408.0 ns   | 104.58 ns   | 120.44 ns | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 882.7 ns     | 17.43 ns  | 17.90 ns  | 3.2 KB    |
| Imposter        | 577.7 ns     | 10.74 ns  | 10.04 ns  | 2.82 KB   |
| Mockolate       | 439.4 ns     | 8.61 ns   | 9.57 ns   | 1.84 KB   |
| Moq             | 195,548.9 ns | 766.29 ns | 679.30 ns | 13.73 KB  |
| NSubstitute     | 5,440.6 ns   | 86.29 ns  | 102.73 ns | 8.41 KB   |
| FakeItEasy      | 6,467.7 ns   | 121.07 ns | 113.25 ns | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-23T02:45:27.613Z*
