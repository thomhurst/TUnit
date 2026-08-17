# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 748.85 ns     | 5.787 ns     | 5.130 ns     | 3008 B    |
| Imposter        | 728.72 ns     | 14.548 ns    | 22.650 ns    | 4688 B    |
| Mockolate       | 418.10 ns     | 7.182 ns     | 6.718 ns     | 2128 B    |
| Moq             | 349,866.50 ns | 1,574.848 ns | 1,473.113 ns | 24325 B   |
| NSubstitute     | 7,085.63 ns   | 61.141 ns    | 51.055 ns    | 10064 B   |
| FakeItEasy      | 7,712.31 ns   | 133.490 ns   | 124.867 ns   | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.22 ns     | 0.482 ns   | 0.403 ns   | 320 B     |
| Imposter        | 328.23 ns    | 2.914 ns   | 2.434 ns   | 2400 B    |
| Mockolate       | 234.01 ns    | 1.659 ns   | 1.385 ns   | 1144 B    |
| Moq             | 90,293.52 ns | 472.785 ns | 442.244 ns | 6918 B    |
| NSubstitute     | 3,738.67 ns  | 23.582 ns  | 18.411 ns  | 7088 B    |
| FakeItEasy      | 3,542.60 ns  | 56.157 ns  | 49.781 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,250.39 ns   | 10.930 ns    | 9.689 ns     | 4472 B    |
| Imposter        | 1,822.01 ns   | 4.968 ns     | 4.404 ns     | 11192 B   |
| Mockolate       | 1,048.45 ns   | 8.133 ns     | 7.608 ns     | 5240 B    |
| Moq             | 477,042.14 ns | 4,811.969 ns | 4,501.119 ns | 34699 B   |
| NSubstitute     | 12,677.49 ns  | 183.405 ns   | 171.557 ns   | 16762 B   |
| FakeItEasy      | 13,732.81 ns  | 227.434 ns   | 233.558 ns   | 19314 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T02:43:20.076Z*
