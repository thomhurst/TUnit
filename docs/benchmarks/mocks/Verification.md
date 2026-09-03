# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 781.86 ns     | 6.261 ns     | 5.228 ns     | 3008 B    |
| Imposter        | 695.69 ns     | 10.457 ns    | 9.782 ns     | 4688 B    |
| Mockolate       | 410.35 ns     | 5.057 ns     | 4.731 ns     | 2128 B    |
| Moq             | 242,949.91 ns | 2,014.815 ns | 1,884.660 ns | 24324 B   |
| NSubstitute     | 6,519.32 ns   | 36.387 ns    | 34.037 ns    | 10064 B   |
| FakeItEasy      | 6,417.06 ns   | 54.468 ns    | 48.284 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 55.89 ns     | 0.196 ns   | 0.164 ns   | 320 B     |
| Imposter        | 327.58 ns    | 4.049 ns   | 3.787 ns   | 2400 B    |
| Mockolate       | 244.00 ns    | 1.769 ns   | 1.568 ns   | 1144 B    |
| Moq             | 61,970.19 ns | 256.978 ns | 227.804 ns | 6925 B    |
| NSubstitute     | 3,645.79 ns  | 24.804 ns  | 21.988 ns  | 7088 B    |
| FakeItEasy      | 3,282.43 ns  | 34.024 ns  | 30.161 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,290.53 ns   | 13.490 ns    | 12.619 ns    | 4472 B    |
| Imposter        | 1,707.52 ns   | 12.782 ns    | 11.331 ns    | 11192 B   |
| Mockolate       | 1,095.07 ns   | 19.568 ns    | 20.938 ns    | 5240 B    |
| Moq             | 351,816.83 ns | 1,511.118 ns | 1,179.782 ns | 34699 B   |
| NSubstitute     | 11,635.22 ns  | 101.948 ns   | 95.362 ns    | 16890 B   |
| FakeItEasy      | 12,142.22 ns  | 239.597 ns   | 246.049 ns   | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-03T02:45:05.205Z*
