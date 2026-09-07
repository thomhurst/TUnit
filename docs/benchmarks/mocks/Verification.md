# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 792.51 ns     | 9.759 ns     | 9.128 ns     | 3008 B    |
| Imposter        | 777.88 ns     | 6.081 ns     | 5.391 ns     | 4688 B    |
| Mockolate       | 416.72 ns     | 3.350 ns     | 3.134 ns     | 2128 B    |
| Moq             | 244,443.23 ns | 1,099.068 ns | 1,028.069 ns | 24324 B   |
| NSubstitute     | 6,854.15 ns   | 27.477 ns    | 24.358 ns    | 10064 B   |
| FakeItEasy      | 6,928.96 ns   | 48.004 ns    | 42.554 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 58.18 ns     | 0.607 ns   | 0.568 ns   | 320 B     |
| Imposter        | 334.99 ns    | 1.835 ns   | 1.717 ns   | 2400 B    |
| Mockolate       | 270.73 ns    | 2.412 ns   | 2.257 ns   | 1144 B    |
| Moq             | 63,407.07 ns | 321.773 ns | 300.986 ns | 6925 B    |
| NSubstitute     | 3,743.53 ns  | 34.710 ns  | 28.984 ns  | 7088 B    |
| FakeItEasy      | 3,392.18 ns  | 66.173 ns  | 64.991 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,319.37 ns   | 10.916 ns    | 10.211 ns    | 4472 B    |
| Imposter        | 1,828.24 ns   | 22.533 ns    | 21.077 ns    | 11192 B   |
| Mockolate       | 1,156.86 ns   | 10.937 ns    | 10.231 ns    | 5240 B    |
| Moq             | 347,076.46 ns | 2,214.819 ns | 1,849.474 ns | 34699 B   |
| NSubstitute     | 11,679.69 ns  | 145.228 ns   | 135.847 ns   | 16763 B   |
| FakeItEasy      | 12,538.31 ns  | 191.344 ns   | 169.621 ns   | 19392 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-07T02:34:20.667Z*
