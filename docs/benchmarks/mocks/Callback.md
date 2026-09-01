# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 672.9 ns     | 4.43 ns   | 3.70 ns   | 3.11 KB   |
| Imposter        | 474.9 ns     | 3.25 ns   | 2.88 ns   | 2.66 KB   |
| Mockolate       | 347.6 ns     | 1.57 ns   | 1.47 ns   | 1.8 KB    |
| Moq             | 136,475.8 ns | 807.47 ns | 755.30 ns | 13.29 KB  |
| NSubstitute     | 4,463.0 ns   | 20.07 ns  | 15.67 ns  | 7.85 KB   |
| FakeItEasy      | 4,559.0 ns   | 74.46 ns  | 69.65 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 838.0 ns     | 5.40 ns     | 5.05 ns   | 3.2 KB    |
| Imposter        | 539.5 ns     | 4.35 ns     | 4.07 ns   | 2.82 KB   |
| Mockolate       | 401.1 ns     | 2.41 ns     | 2.13 ns   | 1.84 KB   |
| Moq             | 143,418.3 ns | 1,066.23 ns | 890.35 ns | 13.73 KB  |
| NSubstitute     | 5,004.5 ns   | 29.81 ns    | 27.88 ns  | 8.41 KB   |
| FakeItEasy      | 5,748.3 ns   | 68.20 ns    | 60.46 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-01T02:34:33.391Z*
