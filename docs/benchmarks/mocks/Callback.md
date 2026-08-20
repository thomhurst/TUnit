# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 646.2 ns     | 1.35 ns     | 1.27 ns     | 3.11 KB   |
| Imposter        | 458.2 ns     | 0.93 ns     | 0.82 ns     | 2.66 KB   |
| Mockolate       | 346.3 ns     | 1.98 ns     | 1.85 ns     | 1.8 KB    |
| Moq             | 136,129.3 ns | 1,581.40 ns | 1,479.24 ns | 13.24 KB  |
| NSubstitute     | 4,399.0 ns   | 18.17 ns    | 14.19 ns    | 7.85 KB   |
| FakeItEasy      | 4,673.8 ns   | 17.45 ns    | 15.47 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 758.4 ns     | 2.97 ns   | 2.78 ns   | 3.2 KB    |
| Imposter        | 535.0 ns     | 1.35 ns   | 1.13 ns   | 2.82 KB   |
| Mockolate       | 392.5 ns     | 2.16 ns   | 1.80 ns   | 1.84 KB   |
| Moq             | 143,740.6 ns | 916.80 ns | 812.72 ns | 13.73 KB  |
| NSubstitute     | 5,042.0 ns   | 14.70 ns  | 12.28 ns  | 8.41 KB   |
| FakeItEasy      | 5,593.6 ns   | 36.77 ns  | 34.40 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-20T02:41:11.657Z*
