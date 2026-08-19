# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 695.10 ns     | 7.572 ns     | 6.712 ns     | 3008 B    |
| Imposter        | 670.32 ns     | 8.133 ns     | 7.608 ns     | 4688 B    |
| Mockolate       | 404.79 ns     | 1.743 ns     | 1.631 ns     | 2128 B    |
| Moq             | 343,662.08 ns | 2,125.162 ns | 1,774.606 ns | 24325 B   |
| NSubstitute     | 6,980.37 ns   | 99.367 ns    | 82.976 ns    | 10064 B   |
| FakeItEasy      | 7,224.13 ns   | 48.335 ns    | 42.848 ns    | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.73 ns     | 0.232 ns   | 0.206 ns   | 320 B     |
| Imposter        | 322.83 ns    | 3.132 ns   | 2.930 ns   | 2400 B    |
| Mockolate       | 245.56 ns    | 4.196 ns   | 4.309 ns   | 1144 B    |
| Moq             | 89,102.83 ns | 808.346 ns | 756.127 ns | 6998 B    |
| NSubstitute     | 4,100.62 ns  | 53.733 ns  | 47.633 ns  | 7088 B    |
| FakeItEasy      | 3,802.84 ns  | 60.445 ns  | 56.540 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,365.39 ns   | 17.064 ns    | 15.962 ns    | 4472 B    |
| Imposter        | 2,022.01 ns   | 30.703 ns    | 28.720 ns    | 11192 B   |
| Mockolate       | 1,142.28 ns   | 5.157 ns     | 4.306 ns     | 5240 B    |
| Moq             | 475,978.55 ns | 2,577.926 ns | 2,285.264 ns | 34699 B   |
| NSubstitute     | 12,134.89 ns  | 132.650 ns   | 117.591 ns   | 16762 B   |
| FakeItEasy      | 13,846.77 ns  | 120.773 ns   | 94.291 ns    | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-19T02:42:18.029Z*
