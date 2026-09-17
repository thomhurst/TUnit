# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 692.03 ns     | 4.649 ns     | 4.349 ns     | 3008 B    |
| Imposter        | 643.19 ns     | 4.247 ns     | 3.973 ns     | 4688 B    |
| Mockolate       | 396.32 ns     | 3.505 ns     | 2.927 ns     | 2128 B    |
| Moq             | 351,259.55 ns | 1,998.611 ns | 1,771.717 ns | 24325 B   |
| NSubstitute     | 7,110.47 ns   | 103.951 ns   | 97.235 ns    | 10064 B   |
| FakeItEasy      | 7,306.01 ns   | 41.422 ns    | 36.719 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 49.92 ns     | 0.369 ns   | 0.345 ns   | 320 B     |
| Imposter        | 312.81 ns    | 2.431 ns   | 2.274 ns   | 2400 B    |
| Mockolate       | 223.05 ns    | 2.474 ns   | 2.066 ns   | 1144 B    |
| Moq             | 89,157.37 ns | 604.490 ns | 535.865 ns | 6918 B    |
| NSubstitute     | 3,779.39 ns  | 46.288 ns  | 43.298 ns  | 7088 B    |
| FakeItEasy      | 3,427.83 ns  | 36.753 ns  | 30.691 ns  | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev     | Allocated |
| --------------- | ------------- | ------------ | ---------- | --------- |
| **TUnit.Mocks** | 1,249.41 ns   | 3.615 ns     | 3.204 ns   | 4472 B    |
| Imposter        | 1,652.11 ns   | 4.490 ns     | 4.200 ns   | 11192 B   |
| Mockolate       | 1,054.37 ns   | 3.069 ns     | 2.721 ns   | 5240 B    |
| Moq             | 474,054.16 ns | 1,184.335 ns | 924.651 ns | 34699 B   |
| NSubstitute     | 11,975.96 ns  | 43.025 ns    | 40.245 ns  | 16762 B   |
| FakeItEasy      | 13,382.54 ns  | 144.003 ns   | 127.655 ns | 19345 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-17T02:33:27.459Z*
