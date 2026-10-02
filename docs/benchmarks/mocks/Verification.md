# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 734.13 ns     | 4.855 ns     | 4.303 ns     | 3024 B    |
| Imposter        | 708.53 ns     | 6.146 ns     | 5.749 ns     | 4688 B    |
| Mockolate       | 405.03 ns     | 4.323 ns     | 3.610 ns     | 2128 B    |
| Moq             | 349,278.41 ns | 3,295.970 ns | 3,083.052 ns | 24325 B   |
| NSubstitute     | 7,086.98 ns   | 24.377 ns    | 21.609 ns    | 10176 B   |
| FakeItEasy      | 7,505.15 ns   | 45.355 ns    | 42.425 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 52.06 ns     | 0.575 ns   | 0.537 ns   | 320 B     |
| Imposter        | 323.03 ns    | 3.014 ns   | 2.819 ns   | 2400 B    |
| Mockolate       | 234.68 ns    | 4.567 ns   | 4.886 ns   | 1144 B    |
| Moq             | 89,788.84 ns | 312.778 ns | 277.269 ns | 6918 B    |
| NSubstitute     | 3,975.00 ns  | 14.003 ns  | 13.099 ns  | 7088 B    |
| FakeItEasy      | 3,575.98 ns  | 33.243 ns  | 31.095 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,246.26 ns   | 24.900 ns    | 32.377 ns    | 4512 B    |
| Imposter        | 1,744.64 ns   | 15.516 ns    | 13.755 ns    | 11192 B   |
| Mockolate       | 1,078.66 ns   | 8.550 ns     | 7.139 ns     | 5240 B    |
| Moq             | 474,689.95 ns | 2,413.732 ns | 2,015.575 ns | 34922 B   |
| NSubstitute     | 12,505.45 ns  | 58.898 ns    | 49.182 ns    | 16763 B   |
| FakeItEasy      | 13,332.57 ns  | 180.519 ns   | 160.025 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-02T02:44:13.542Z*
