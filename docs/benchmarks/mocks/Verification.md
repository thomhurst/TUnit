# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 635.94 ns     | 5.518 ns     | 5.162 ns     | 3024 B    |
| Imposter        | 596.05 ns     | 8.222 ns     | 7.288 ns     | 4664 B    |
| Mockolate       | 374.06 ns     | 3.807 ns     | 3.561 ns     | 2128 B    |
| Moq             | 151,932.77 ns | 2,427.111 ns | 2,270.321 ns | 24338 B   |
| NSubstitute     | 5,724.29 ns   | 108.274 ns   | 120.346 ns   | 10176 B   |
| FakeItEasy      | 4,558.79 ns   | 62.631 ns    | 58.585 ns    | 10717 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 43.72 ns     | 0.407 ns   | 0.381 ns   | 320 B     |
| Imposter        | 258.09 ns    | 3.756 ns   | 3.329 ns   | 2384 B    |
| Mockolate       | 200.22 ns    | 2.201 ns   | 2.059 ns   | 1144 B    |
| Moq             | 37,763.88 ns | 418.517 ns | 391.481 ns | 6922 B    |
| NSubstitute     | 3,047.12 ns  | 33.861 ns  | 31.674 ns  | 7088 B    |
| FakeItEasy      | 2,124.92 ns  | 29.919 ns  | 24.984 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,097.56 ns   | 13.279 ns    | 12.421 ns    | 4512 B    |
| Imposter        | 1,413.35 ns   | 7.962 ns     | 7.448 ns     | 11144 B   |
| Mockolate       | 894.90 ns     | 7.499 ns     | 6.647 ns     | 5240 B    |
| Moq             | 192,127.04 ns | 3,498.959 ns | 3,101.736 ns | 34584 B   |
| NSubstitute     | 9,616.41 ns   | 61.238 ns    | 57.282 ns    | 16760 B   |
| FakeItEasy      | 7,925.50 ns   | 131.659 ns   | 123.154 ns   | 19238 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-10T02:40:36.038Z*
