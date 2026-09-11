# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 784.28 ns     | 4.825 ns     | 4.277 ns     | 3008 B    |
| Imposter        | 714.22 ns     | 6.315 ns     | 5.907 ns     | 4688 B    |
| Mockolate       | 395.99 ns     | 3.085 ns     | 2.734 ns     | 2128 B    |
| Moq             | 245,176.36 ns | 1,504.789 ns | 1,256.567 ns | 24324 B   |
| NSubstitute     | 6,659.63 ns   | 41.710 ns    | 34.830 ns    | 10064 B   |
| FakeItEasy      | 6,723.88 ns   | 37.428 ns    | 35.010 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 54.87 ns     | 0.482 ns   | 0.402 ns   | 320 B     |
| Imposter        | 329.60 ns    | 1.112 ns   | 0.929 ns   | 2400 B    |
| Mockolate       | 238.01 ns    | 1.508 ns   | 1.411 ns   | 1144 B    |
| Moq             | 62,930.27 ns | 406.123 ns | 360.018 ns | 7005 B    |
| NSubstitute     | 3,642.63 ns  | 28.015 ns  | 21.872 ns  | 7088 B    |
| FakeItEasy      | 3,275.96 ns  | 63.835 ns  | 59.711 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,276.20 ns   | 15.733 ns    | 14.717 ns    | 4472 B    |
| Imposter        | 1,758.57 ns   | 25.766 ns    | 24.101 ns    | 11192 B   |
| Mockolate       | 1,118.68 ns   | 21.071 ns    | 19.710 ns    | 5240 B    |
| Moq             | 354,967.63 ns | 3,699.360 ns | 3,279.386 ns | 34922 B   |
| NSubstitute     | 11,380.16 ns  | 84.095 ns    | 70.223 ns    | 16762 B   |
| FakeItEasy      | 11,893.15 ns  | 180.580 ns   | 168.915 ns   | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-11T02:38:48.126Z*
