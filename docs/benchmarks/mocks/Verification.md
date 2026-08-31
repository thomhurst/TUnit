# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 702.44 ns     | 11.414 ns    | 14.841 ns    | 3008 B    |
| Imposter        | 643.11 ns     | 12.735 ns    | 22.304 ns    | 4688 B    |
| Mockolate       | 409.17 ns     | 7.618 ns     | 6.753 ns     | 2128 B    |
| Moq             | 156,456.69 ns | 1,949.864 ns | 1,628.224 ns | 24338 B   |
| NSubstitute     | 6,053.22 ns   | 58.090 ns    | 51.495 ns    | 10064 B   |
| FakeItEasy      | 5,152.58 ns   | 78.100 ns    | 83.566 ns    | 10717 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 58.03 ns     | 1.191 ns   | 2.266 ns   | 320 B     |
| Imposter        | 299.52 ns    | 5.829 ns   | 5.452 ns   | 2400 B    |
| Mockolate       | 258.65 ns    | 5.458 ns   | 16.092 ns  | 1144 B    |
| Moq             | 40,073.15 ns | 666.258 ns | 623.218 ns | 6922 B    |
| NSubstitute     | 3,381.60 ns  | 26.583 ns  | 24.866 ns  | 7088 B    |
| FakeItEasy      | 2,571.21 ns  | 24.748 ns  | 23.149 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,298.13 ns   | 18.942 ns    | 19.452 ns    | 4472 B    |
| Imposter        | 1,807.49 ns   | 34.610 ns    | 45.003 ns    | 11192 B   |
| Mockolate       | 1,125.06 ns   | 22.315 ns    | 36.035 ns    | 5240 B    |
| Moq             | 195,535.17 ns | 3,578.309 ns | 3,172.078 ns | 34584 B   |
| NSubstitute     | 11,530.97 ns  | 220.659 ns   | 206.404 ns   | 16760 B   |
| FakeItEasy      | 9,681.26 ns   | 82.356 ns    | 77.036 ns    | 19246 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
