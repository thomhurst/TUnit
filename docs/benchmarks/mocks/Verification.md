# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 757.23 ns     | 15.166 ns    | 23.160 ns    | 3008 B    |
| Imposter        | 793.31 ns     | 10.345 ns    | 9.677 ns     | 4688 B    |
| Mockolate       | 455.60 ns     | 4.863 ns     | 4.549 ns     | 2128 B    |
| Moq             | 345,947.46 ns | 1,526.832 ns | 1,353.497 ns | 24325 B   |
| NSubstitute     | 7,113.51 ns   | 33.398 ns    | 29.607 ns    | 10064 B   |
| FakeItEasy      | 7,883.26 ns   | 21.699 ns    | 19.236 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 60.56 ns     | 0.621 ns   | 0.581 ns   | 320 B     |
| Imposter        | 391.79 ns    | 6.510 ns   | 6.090 ns   | 2400 B    |
| Mockolate       | 273.21 ns    | 2.728 ns   | 2.552 ns   | 1144 B    |
| Moq             | 88,586.43 ns | 518.130 ns | 432.662 ns | 6918 B    |
| NSubstitute     | 4,049.92 ns  | 19.253 ns  | 18.010 ns  | 7088 B    |
| FakeItEasy      | 3,848.64 ns  | 22.021 ns  | 19.521 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,385.15 ns   | 15.849 ns    | 14.825 ns    | 4472 B    |
| Imposter        | 1,978.57 ns   | 32.345 ns    | 30.256 ns    | 11192 B   |
| Mockolate       | 1,214.63 ns   | 13.041 ns    | 11.561 ns    | 5240 B    |
| Moq             | 480,368.69 ns | 2,583.062 ns | 2,289.817 ns | 34699 B   |
| NSubstitute     | 12,871.70 ns  | 72.647 ns    | 60.664 ns    | 16763 B   |
| FakeItEasy      | 13,998.03 ns  | 120.988 ns   | 113.172 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-30T02:44:44.759Z*
