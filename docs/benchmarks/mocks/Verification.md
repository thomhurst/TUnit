# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 678.86 ns     | 1.976 ns     | 1.752 ns     | 3008 B    |
| Imposter        | 671.57 ns     | 1.453 ns     | 1.359 ns     | 4688 B    |
| Mockolate       | 382.04 ns     | 1.003 ns     | 0.889 ns     | 2128 B    |
| Moq             | 346,367.42 ns | 2,724.672 ns | 2,548.660 ns | 24325 B   |
| NSubstitute     | 6,822.60 ns   | 81.736 ns    | 68.253 ns    | 10064 B   |
| FakeItEasy      | 7,175.01 ns   | 27.702 ns    | 24.557 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.08 ns     | 0.343 ns   | 0.321 ns   | 320 B     |
| Imposter        | 327.54 ns    | 0.813 ns   | 0.761 ns   | 2400 B    |
| Mockolate       | 229.82 ns    | 0.524 ns   | 0.464 ns   | 1144 B    |
| Moq             | 87,859.01 ns | 519.349 ns | 485.799 ns | 6918 B    |
| NSubstitute     | 3,738.77 ns  | 9.842 ns   | 8.725 ns   | 7088 B    |
| FakeItEasy      | 3,539.39 ns  | 34.855 ns  | 30.898 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,214.15 ns   | 3.496 ns     | 3.099 ns     | 4472 B    |
| Imposter        | 1,716.74 ns   | 5.901 ns     | 5.519 ns     | 11192 B   |
| Mockolate       | 1,041.64 ns   | 2.369 ns     | 1.978 ns     | 5240 B    |
| Moq             | 471,265.94 ns | 2,301.865 ns | 2,040.543 ns | 34811 B   |
| NSubstitute     | 12,465.25 ns  | 51.462 ns    | 45.620 ns    | 16889 B   |
| FakeItEasy      | 13,246.06 ns  | 137.703 ns   | 114.989 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-21T02:46:27.792Z*
