# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev     | Allocated |
| --------------- | ------------- | ------------ | ---------- | --------- |
| **TUnit.Mocks** | 440.48 ns     | 7.705 ns     | 6.830 ns   | 3008 B    |
| Imposter        | 376.01 ns     | 7.012 ns     | 8.347 ns   | 4688 B    |
| Mockolate       | 246.60 ns     | 2.675 ns     | 2.502 ns   | 2128 B    |
| Moq             | 108,344.12 ns | 1,190.311 ns | 993.964 ns | 24340 B   |
| NSubstitute     | 3,530.05 ns   | 64.137 ns    | 56.855 ns  | 10064 B   |
| FakeItEasy      | 3,566.14 ns   | 41.114 ns    | 38.458 ns  | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 32.68 ns     | 0.563 ns   | 0.527 ns   | 320 B     |
| Imposter        | 173.57 ns    | 2.116 ns   | 1.875 ns   | 2400 B    |
| Mockolate       | 136.28 ns    | 2.152 ns   | 1.908 ns   | 1144 B    |
| Moq             | 26,920.63 ns | 256.781 ns | 227.630 ns | 6925 B    |
| NSubstitute     | 1,923.76 ns  | 15.942 ns  | 12.446 ns  | 7088 B    |
| FakeItEasy      | 1,874.41 ns  | 33.616 ns  | 29.800 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 754.70 ns     | 7.535 ns     | 6.292 ns     | 4472 B    |
| Imposter        | 976.70 ns     | 13.380 ns    | 12.516 ns    | 11192 B   |
| Mockolate       | 637.50 ns     | 12.457 ns    | 13.329 ns    | 5240 B    |
| Moq             | 145,914.45 ns | 2,916.790 ns | 2,864.678 ns | 34698 B   |
| NSubstitute     | 6,421.25 ns   | 125.627 ns   | 111.365 ns   | 16761 B   |
| FakeItEasy      | 6,540.16 ns   | 123.346 ns   | 115.378 ns   | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-22T02:40:44.558Z*
