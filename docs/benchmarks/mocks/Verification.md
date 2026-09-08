# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev     | Allocated |
| --------------- | ------------- | ------------ | ---------- | --------- |
| **TUnit.Mocks** | 720.66 ns     | 6.668 ns     | 6.238 ns   | 3008 B    |
| Imposter        | 695.97 ns     | 10.325 ns    | 9.658 ns   | 4688 B    |
| Mockolate       | 400.70 ns     | 2.111 ns     | 1.975 ns   | 2128 B    |
| Moq             | 243,020.67 ns | 1,117.495 ns | 990.630 ns | 24324 B   |
| NSubstitute     | 6,425.99 ns   | 76.669 ns    | 71.716 ns  | 10064 B   |
| FakeItEasy      | 6,431.31 ns   | 40.431 ns    | 37.819 ns  | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 60.26 ns     | 0.980 ns   | 0.916 ns   | 320 B     |
| Imposter        | 322.45 ns    | 1.027 ns   | 0.911 ns   | 2400 B    |
| Mockolate       | 239.36 ns    | 0.593 ns   | 0.555 ns   | 1144 B    |
| Moq             | 61,410.23 ns | 342.675 ns | 286.149 ns | 6925 B    |
| NSubstitute     | 3,543.25 ns  | 8.277 ns   | 7.337 ns   | 7088 B    |
| FakeItEasy      | 3,198.46 ns  | 20.752 ns  | 18.396 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,271.90 ns   | 4.787 ns     | 4.244 ns     | 4472 B    |
| Imposter        | 1,664.66 ns   | 4.586 ns     | 4.065 ns     | 11192 B   |
| Mockolate       | 1,152.22 ns   | 3.417 ns     | 3.029 ns     | 5240 B    |
| Moq             | 346,196.73 ns | 3,188.337 ns | 2,826.378 ns | 34699 B   |
| NSubstitute     | 11,239.48 ns  | 31.074 ns    | 27.546 ns    | 16762 B   |
| FakeItEasy      | 11,466.49 ns  | 95.406 ns    | 84.575 ns    | 19232 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-08T02:32:39.573Z*
