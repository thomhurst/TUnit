# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 752.08 ns     | 14.565 ns    | 14.305 ns    | 3008 B    |
| Imposter        | 733.43 ns     | 8.682 ns     | 7.250 ns     | 4688 B    |
| Mockolate       | 425.97 ns     | 8.296 ns     | 10.491 ns    | 2128 B    |
| Moq             | 343,393.64 ns | 1,660.046 ns | 1,296.055 ns | 24325 B   |
| NSubstitute     | 7,194.98 ns   | 70.799 ns    | 66.226 ns    | 10064 B   |
| FakeItEasy      | 8,201.11 ns   | 74.885 ns    | 70.048 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 60.45 ns     | 0.586 ns   | 0.519 ns   | 320 B     |
| Imposter        | 391.30 ns    | 6.788 ns   | 6.350 ns   | 2400 B    |
| Mockolate       | 238.81 ns    | 4.824 ns   | 12.875 ns  | 1144 B    |
| Moq             | 87,318.61 ns | 394.964 ns | 329.813 ns | 6918 B    |
| NSubstitute     | 3,789.74 ns  | 17.798 ns  | 15.777 ns  | 7088 B    |
| FakeItEasy      | 3,583.02 ns  | 31.450 ns  | 27.880 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,241.51 ns   | 12.647 ns    | 11.211 ns    | 4472 B    |
| Imposter        | 1,733.90 ns   | 12.218 ns    | 11.429 ns    | 11192 B   |
| Mockolate       | 1,080.70 ns   | 10.710 ns    | 10.018 ns    | 5240 B    |
| Moq             | 479,335.15 ns | 1,899.719 ns | 1,776.999 ns | 35130 B   |
| NSubstitute     | 12,201.92 ns  | 36.627 ns    | 30.586 ns    | 16762 B   |
| FakeItEasy      | 13,539.02 ns  | 181.343 ns   | 169.629 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-28T05:02:48.374Z*
