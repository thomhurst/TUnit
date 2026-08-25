# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 650.3 ns     | 3.15 ns     | 2.95 ns     | 3.11 KB   |
| Imposter        | 450.4 ns     | 1.60 ns     | 1.50 ns     | 2.66 KB   |
| Mockolate       | 325.5 ns     | 1.40 ns     | 1.31 ns     | 1.8 KB    |
| Moq             | 180,524.1 ns | 1,287.18 ns | 1,204.03 ns | 13.14 KB  |
| NSubstitute     | 4,779.4 ns   | 18.54 ns    | 16.43 ns    | 7.85 KB   |
| FakeItEasy      | 5,019.8 ns   | 20.93 ns    | 18.55 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 766.6 ns     | 1.81 ns   | 1.60 ns   | 3.2 KB    |
| Imposter        | 520.8 ns     | 1.16 ns   | 1.09 ns   | 2.82 KB   |
| Mockolate       | 399.1 ns     | 1.54 ns   | 1.44 ns   | 1.84 KB   |
| Moq             | 189,004.0 ns | 945.52 ns | 838.18 ns | 13.73 KB  |
| NSubstitute     | 5,342.8 ns   | 42.40 ns  | 37.59 ns  | 8.41 KB   |
| FakeItEasy      | 6,223.2 ns   | 65.02 ns  | 57.64 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-25T02:41:00.074Z*
