# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 805.88 ns     | 13.506 ns    | 12.634 ns    | 3008 B    |
| Imposter        | 764.85 ns     | 14.802 ns    | 21.228 ns    | 4688 B    |
| Mockolate       | 405.88 ns     | 5.188 ns     | 4.853 ns     | 2128 B    |
| Moq             | 248,889.64 ns | 3,273.883 ns | 2,902.212 ns | 24324 B   |
| NSubstitute     | 6,641.62 ns   | 126.665 ns   | 160.191 ns   | 10064 B   |
| FakeItEasy      | 6,826.65 ns   | 134.862 ns   | 189.058 ns   | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 57.39 ns     | 0.762 ns   | 0.713 ns   | 320 B     |
| Imposter        | 351.91 ns    | 6.950 ns   | 9.277 ns   | 2400 B    |
| Mockolate       | 244.27 ns    | 2.518 ns   | 2.103 ns   | 1144 B    |
| Moq             | 64,360.35 ns | 619.472 ns | 483.643 ns | 7037 B    |
| NSubstitute     | 3,690.99 ns  | 73.790 ns  | 82.017 ns  | 7088 B    |
| FakeItEasy      | 3,403.23 ns  | 58.478 ns  | 54.701 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev     | Allocated |
| --------------- | ------------- | ------------ | ---------- | --------- |
| **TUnit.Mocks** | 1,337.05 ns   | 24.017 ns    | 22.465 ns  | 4472 B    |
| Imposter        | 1,822.91 ns   | 35.972 ns    | 80.457 ns  | 11192 B   |
| Mockolate       | 1,162.26 ns   | 23.046 ns    | 40.362 ns  | 5240 B    |
| Moq             | 345,620.84 ns | 1,077.065 ns | 840.901 ns | 34699 B   |
| NSubstitute     | 11,711.81 ns  | 161.307 ns   | 150.887 ns | 16762 B   |
| FakeItEasy      | 12,091.56 ns  | 240.054 ns   | 266.819 ns | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-27T04:05:27.840Z*
