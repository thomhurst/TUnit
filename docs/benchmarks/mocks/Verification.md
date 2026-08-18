# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 691.68 ns     | 4.650 ns     | 3.883 ns     | 3008 B    |
| Imposter        | 682.23 ns     | 5.297 ns     | 4.955 ns     | 4688 B    |
| Mockolate       | 399.46 ns     | 2.776 ns     | 2.168 ns     | 2128 B    |
| Moq             | 340,102.89 ns | 2,763.892 ns | 2,450.119 ns | 24325 B   |
| NSubstitute     | 6,792.81 ns   | 36.707 ns    | 28.658 ns    | 10064 B   |
| FakeItEasy      | 7,257.17 ns   | 94.805 ns    | 84.042 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.64 ns     | 0.295 ns   | 0.262 ns   | 320 B     |
| Imposter        | 329.91 ns    | 4.760 ns   | 4.220 ns   | 2400 B    |
| Mockolate       | 238.66 ns    | 2.282 ns   | 2.023 ns   | 1144 B    |
| Moq             | 86,718.89 ns | 840.348 ns | 786.062 ns | 6918 B    |
| NSubstitute     | 3,761.79 ns  | 21.072 ns  | 18.680 ns  | 7088 B    |
| FakeItEasy      | 3,579.36 ns  | 39.108 ns  | 34.668 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,240.92 ns   | 3.747 ns     | 3.129 ns     | 4472 B    |
| Imposter        | 1,738.64 ns   | 14.135 ns    | 13.222 ns    | 11192 B   |
| Mockolate       | 1,078.38 ns   | 8.332 ns     | 7.386 ns     | 5240 B    |
| Moq             | 469,618.42 ns | 1,959.407 ns | 1,736.964 ns | 34699 B   |
| NSubstitute     | 12,177.90 ns  | 190.223 ns   | 168.628 ns   | 16763 B   |
| FakeItEasy      | 13,284.98 ns  | 154.494 ns   | 129.009 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-18T02:39:29.373Z*
