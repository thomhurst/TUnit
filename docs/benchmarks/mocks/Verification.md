# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 695.88 ns     | 8.801 ns     | 8.232 ns     | 3008 B    |
| Imposter        | 705.19 ns     | 6.842 ns     | 5.714 ns     | 4688 B    |
| Mockolate       | 422.46 ns     | 3.514 ns     | 3.115 ns     | 2128 B    |
| Moq             | 351,324.77 ns | 1,930.234 ns | 1,805.542 ns | 24325 B   |
| NSubstitute     | 7,107.63 ns   | 63.421 ns    | 59.324 ns    | 10064 B   |
| FakeItEasy      | 7,895.17 ns   | 102.543 ns   | 95.919 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 51.78 ns     | 0.816 ns   | 0.764 ns   | 320 B     |
| Imposter        | 335.88 ns    | 6.137 ns   | 5.740 ns   | 2400 B    |
| Mockolate       | 239.11 ns    | 3.531 ns   | 3.130 ns   | 1144 B    |
| Moq             | 90,451.07 ns | 226.176 ns | 200.499 ns | 6918 B    |
| NSubstitute     | 3,846.98 ns  | 46.239 ns  | 43.252 ns  | 7088 B    |
| FakeItEasy      | 3,852.37 ns  | 38.524 ns  | 34.151 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,315.71 ns   | 15.436 ns    | 13.684 ns    | 4472 B    |
| Imposter        | 1,848.88 ns   | 36.988 ns    | 65.747 ns    | 11192 B   |
| Mockolate       | 1,157.64 ns   | 23.035 ns    | 45.469 ns    | 5240 B    |
| Moq             | 489,735.48 ns | 5,964.318 ns | 5,287.213 ns | 35098 B   |
| NSubstitute     | 12,552.98 ns  | 248.036 ns   | 254.714 ns   | 16891 B   |
| FakeItEasy      | 13,990.18 ns  | 260.890 ns   | 267.914 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-12T02:33:29.738Z*
