# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 642.56 ns     | 10.228 ns  | 12.935 ns  | 3040 B    |
| Imposter        | 245.61 ns     | 4.884 ns   | 8.681 ns   | 1096 B    |
| Mockolate       | 354.26 ns     | 5.925 ns   | 6.085 ns   | 1824 B    |
| Moq             | 140,787.42 ns | 756.447 ns | 707.581 ns | 24314 B   |
| NSubstitute     | 5,156.06 ns   | 79.997 ns  | 74.829 ns  | 10024 B   |
| FakeItEasy      | 4,143.91 ns   | 53.651 ns  | 47.560 ns  | 10501 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 46.55 ns     | 0.895 ns   | 1.066 ns   | 336 B     |
| Imposter        | 119.62 ns    | 2.390 ns   | 4.489 ns   | 616 B     |
| Mockolate       | 199.28 ns    | 3.957 ns   | 4.711 ns   | 1040 B    |
| Moq             | 34,044.10 ns | 190.048 ns | 168.473 ns | 6914 B    |
| NSubstitute     | 2,506.69 ns  | 26.173 ns  | 24.482 ns  | 7056 B    |
| FakeItEasy      | 2,134.31 ns  | 14.787 ns  | 13.108 ns  | 5118 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,031.30 ns   | 8.477 ns     | 7.079 ns     | 4528 B    |
| Imposter        | 467.11 ns     | 3.228 ns     | 2.862 ns     | 2288 B    |
| Mockolate       | 696.47 ns     | 11.345 ns    | 10.612 ns    | 3264 B    |
| Moq             | 180,659.33 ns | 1,238.485 ns | 1,158.480 ns | 34847 B   |
| NSubstitute     | 8,275.87 ns   | 137.386 ns   | 121.789 ns   | 16633 B   |
| FakeItEasy      | 7,510.00 ns   | 145.183 ns   | 198.728 ns   | 18716 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-17T02:23:59.645Z*
