# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 671.0 ns     | 7.47 ns     | 5.83 ns     | 3.11 KB   |
| Imposter        | 446.1 ns     | 2.16 ns     | 1.81 ns     | 2.66 KB   |
| Mockolate       | 349.0 ns     | 2.41 ns     | 2.14 ns     | 1.8 KB    |
| Moq             | 187,181.2 ns | 1,699.55 ns | 1,589.76 ns | 13.26 KB  |
| NSubstitute     | 5,018.0 ns   | 99.28 ns    | 97.51 ns    | 7.85 KB   |
| FakeItEasy      | 5,318.4 ns   | 50.11 ns    | 46.87 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 774.2 ns     | 11.92 ns  | 11.15 ns  | 3.2 KB    |
| Imposter        | 517.7 ns     | 5.38 ns   | 5.03 ns   | 2.82 KB   |
| Mockolate       | 385.2 ns     | 4.24 ns   | 3.97 ns   | 1.84 KB   |
| Moq             | 193,887.9 ns | 854.70 ns | 757.67 ns | 13.73 KB  |
| NSubstitute     | 5,584.4 ns   | 31.67 ns  | 29.62 ns  | 8.41 KB   |
| FakeItEasy      | 6,466.2 ns   | 87.44 ns  | 77.52 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-11T02:38:48.126Z*
