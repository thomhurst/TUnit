# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 529.6 ns     | 10.50 ns  | 9.82 ns   | 3.12 KB   |
| Imposter        | 380.7 ns     | 1.66 ns   | 1.39 ns   | 2.64 KB   |
| Mockolate       | 265.8 ns     | 1.97 ns   | 1.84 ns   | 1.8 KB    |
| Moq             | 106,858.0 ns | 314.69 ns | 278.96 ns | 13.29 KB  |
| NSubstitute     | 3,567.8 ns   | 52.72 ns  | 46.73 ns  | 7.85 KB   |
| FakeItEasy      | 3,759.4 ns   | 69.73 ns  | 65.23 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 646.8 ns     | 12.97 ns    | 15.92 ns    | 3.21 KB   |
| Imposter        | 443.0 ns     | 5.18 ns     | 4.59 ns     | 2.8 KB    |
| Mockolate       | 305.1 ns     | 2.46 ns     | 2.18 ns     | 1.84 KB   |
| Moq             | 114,103.6 ns | 1,261.84 ns | 1,118.59 ns | 13.72 KB  |
| NSubstitute     | 4,105.2 ns   | 35.91 ns    | 33.59 ns    | 8.41 KB   |
| FakeItEasy      | 4,619.6 ns   | 33.27 ns    | 31.12 ns    | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-10T02:40:36.038Z*
