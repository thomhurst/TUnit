# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 710.38 ns     | 6.249 ns     | 5.540 ns     | 3008 B    |
| Imposter        | 719.70 ns     | 12.787 ns    | 17.926 ns    | 4688 B    |
| Mockolate       | 415.98 ns     | 5.417 ns     | 5.067 ns     | 2128 B    |
| Moq             | 351,577.25 ns | 2,449.843 ns | 2,171.722 ns | 24325 B   |
| NSubstitute     | 7,029.95 ns   | 72.824 ns    | 68.120 ns    | 10064 B   |
| FakeItEasy      | 7,821.32 ns   | 22.740 ns    | 20.158 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 54.15 ns     | 1.007 ns   | 0.942 ns   | 320 B     |
| Imposter        | 340.61 ns    | 6.777 ns   | 12.562 ns  | 2400 B    |
| Mockolate       | 250.13 ns    | 5.024 ns   | 5.160 ns   | 1144 B    |
| Moq             | 90,957.94 ns | 934.469 ns | 874.103 ns | 6918 B    |
| NSubstitute     | 3,897.18 ns  | 19.088 ns  | 17.855 ns  | 7088 B    |
| FakeItEasy      | 3,769.08 ns  | 31.141 ns  | 27.606 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,312.16 ns   | 12.577 ns    | 11.764 ns    | 4472 B    |
| Imposter        | 1,877.09 ns   | 32.678 ns    | 28.968 ns    | 11192 B   |
| Mockolate       | 1,193.30 ns   | 10.924 ns    | 9.684 ns     | 5240 B    |
| Moq             | 482,961.94 ns | 3,184.188 ns | 2,978.491 ns | 34699 B   |
| NSubstitute     | 12,670.56 ns  | 127.948 ns   | 119.682 ns   | 16763 B   |
| FakeItEasy      | 13,901.63 ns  | 131.165 ns   | 116.274 ns   | 19393 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-24T02:33:35.117Z*
