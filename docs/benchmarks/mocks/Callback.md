# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 700.5 ns     | 4.89 ns     | 4.58 ns     | 3.11 KB   |
| Imposter        | 489.3 ns     | 5.79 ns     | 4.84 ns     | 2.66 KB   |
| Mockolate       | 353.4 ns     | 3.52 ns     | 3.12 ns     | 1.8 KB    |
| Moq             | 135,820.9 ns | 1,362.78 ns | 1,137.98 ns | 13.29 KB  |
| NSubstitute     | 4,704.0 ns   | 61.33 ns    | 51.21 ns    | 7.85 KB   |
| FakeItEasy      | 4,698.6 ns   | 73.85 ns    | 65.47 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 802.6 ns     | 10.50 ns    | 9.82 ns     | 3.2 KB    |
| Imposter        | 538.3 ns     | 3.07 ns     | 2.57 ns     | 2.82 KB   |
| Mockolate       | 401.3 ns     | 3.33 ns     | 2.96 ns     | 1.84 KB   |
| Moq             | 141,873.7 ns | 1,131.75 ns | 1,003.26 ns | 13.73 KB  |
| NSubstitute     | 5,224.2 ns   | 54.85 ns    | 51.31 ns    | 8.41 KB   |
| FakeItEasy      | 5,889.5 ns   | 115.27 ns   | 123.34 ns   | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-02T02:49:53.672Z*
