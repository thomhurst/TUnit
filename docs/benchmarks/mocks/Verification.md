# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 746.89 ns     | 10.915 ns    | 8.522 ns     | 3008 B    |
| Imposter        | 718.60 ns     | 14.349 ns    | 31.193 ns    | 4688 B    |
| Mockolate       | 407.63 ns     | 4.472 ns     | 4.183 ns     | 2128 B    |
| Moq             | 244,614.20 ns | 2,885.150 ns | 2,409.231 ns | 24324 B   |
| NSubstitute     | 6,487.99 ns   | 74.147 ns    | 65.730 ns    | 10064 B   |
| FakeItEasy      | 6,429.72 ns   | 105.358 ns   | 98.552 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 53.93 ns     | 0.383 ns   | 0.358 ns   | 320 B     |
| Imposter        | 329.59 ns    | 4.781 ns   | 4.472 ns   | 2400 B    |
| Mockolate       | 237.65 ns    | 1.528 ns   | 1.355 ns   | 1144 B    |
| Moq             | 63,150.84 ns | 304.935 ns | 254.635 ns | 6925 B    |
| NSubstitute     | 3,559.96 ns  | 39.122 ns  | 34.681 ns  | 7088 B    |
| FakeItEasy      | 3,187.52 ns  | 49.203 ns  | 46.024 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,304.87 ns   | 19.248 ns    | 18.004 ns    | 4472 B    |
| Imposter        | 1,665.51 ns   | 12.112 ns    | 11.330 ns    | 11192 B   |
| Mockolate       | 1,043.86 ns   | 2.627 ns     | 2.193 ns     | 5240 B    |
| Moq             | 352,948.79 ns | 2,522.480 ns | 2,236.113 ns | 34811 B   |
| NSubstitute     | 11,407.85 ns  | 144.003 ns   | 134.701 ns   | 16762 B   |
| FakeItEasy      | 11,419.37 ns  | 75.510 ns    | 66.937 ns    | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T02:35:19.410Z*
