# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 905.98 ns     | 6.020 ns     | 5.631 ns     | 3024 B    |
| Imposter        | 939.87 ns     | 8.148 ns     | 7.223 ns     | 4664 B    |
| Mockolate       | 558.25 ns     | 3.851 ns     | 3.602 ns     | 2128 B    |
| Moq             | 171,921.64 ns | 1,155.964 ns | 1,024.732 ns | 24482 B   |
| NSubstitute     | 7,053.30 ns   | 36.413 ns    | 34.061 ns    | 10064 B   |
| FakeItEasy      | 6,073.60 ns   | 48.172 ns    | 42.703 ns    | 10719 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 69.30 ns     | 0.224 ns   | 0.210 ns   | 320 B     |
| Imposter        | 442.48 ns    | 8.161 ns   | 7.633 ns   | 2384 B    |
| Mockolate       | 290.60 ns    | 2.356 ns   | 2.204 ns   | 1144 B    |
| Moq             | 43,709.12 ns | 163.796 ns | 145.201 ns | 6904 B    |
| NSubstitute     | 3,675.08 ns  | 18.374 ns  | 15.343 ns  | 7088 B    |
| FakeItEasy      | 2,892.67 ns  | 20.206 ns  | 17.912 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 1,470.46 ns   | 4.227 ns   | 3.747 ns   | 4512 B    |
| Imposter        | 2,144.15 ns   | 7.493 ns   | 6.642 ns   | 11144 B   |
| Mockolate       | 1,335.75 ns   | 15.121 ns  | 14.144 ns  | 5240 B    |
| Moq             | 225,203.44 ns | 902.248 ns | 843.963 ns | 34584 B   |
| NSubstitute     | 12,585.10 ns  | 66.886 ns  | 59.293 ns  | 16760 B   |
| FakeItEasy      | 10,697.18 ns  | 116.383 ns | 97.185 ns  | 19238 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-09T02:42:26.464Z*
