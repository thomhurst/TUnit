# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 757.38 ns     | 13.431 ns    | 12.563 ns    | 3008 B    |
| Imposter        | 730.08 ns     | 12.371 ns    | 10.966 ns    | 4688 B    |
| Mockolate       | 404.55 ns     | 1.474 ns     | 1.231 ns     | 2128 B    |
| Moq             | 347,942.95 ns | 2,038.898 ns | 1,807.430 ns | 24325 B   |
| NSubstitute     | 7,076.23 ns   | 77.488 ns    | 72.482 ns    | 10064 B   |
| FakeItEasy      | 7,606.75 ns   | 33.969 ns    | 30.113 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 53.26 ns     | 0.591 ns   | 0.524 ns   | 320 B     |
| Imposter        | 337.80 ns    | 2.597 ns   | 2.429 ns   | 2400 B    |
| Mockolate       | 244.92 ns    | 4.931 ns   | 4.843 ns   | 1144 B    |
| Moq             | 89,034.99 ns | 249.542 ns | 208.379 ns | 6918 B    |
| NSubstitute     | 3,984.18 ns  | 48.944 ns  | 45.782 ns  | 7088 B    |
| FakeItEasy      | 3,722.70 ns  | 71.592 ns  | 70.313 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,280.87 ns   | 10.870 ns    | 10.168 ns    | 4472 B    |
| Imposter        | 1,849.21 ns   | 27.266 ns    | 25.505 ns    | 11192 B   |
| Mockolate       | 1,129.89 ns   | 13.561 ns    | 12.685 ns    | 5240 B    |
| Moq             | 484,328.25 ns | 3,701.225 ns | 3,462.128 ns | 34922 B   |
| NSubstitute     | 13,146.88 ns  | 106.801 ns   | 99.902 ns    | 16763 B   |
| FakeItEasy      | 14,006.14 ns  | 218.499 ns   | 182.456 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-24T02:46:06.016Z*
