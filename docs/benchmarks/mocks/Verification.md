# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 702.47 ns     | 3.495 ns     | 2.918 ns     | 3008 B    |
| Imposter        | 688.05 ns     | 4.026 ns     | 3.766 ns     | 4688 B    |
| Mockolate       | 406.50 ns     | 1.384 ns     | 1.227 ns     | 2128 B    |
| Moq             | 347,190.73 ns | 1,895.850 ns | 1,680.621 ns | 24325 B   |
| NSubstitute     | 6,816.33 ns   | 64.578 ns    | 60.407 ns    | 10064 B   |
| FakeItEasy      | 7,518.42 ns   | 124.317 ns   | 110.204 ns   | 10724 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.43 ns     | 0.108 ns   | 0.096 ns   | 320 B     |
| Imposter        | 323.83 ns    | 1.545 ns   | 1.369 ns   | 2400 B    |
| Mockolate       | 224.77 ns    | 1.296 ns   | 1.149 ns   | 1144 B    |
| Moq             | 90,334.21 ns | 293.895 ns | 245.416 ns | 7030 B    |
| NSubstitute     | 3,811.39 ns  | 53.261 ns  | 49.820 ns  | 7088 B    |
| FakeItEasy      | 3,734.17 ns  | 22.847 ns  | 20.253 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,227.53 ns   | 7.414 ns     | 6.573 ns     | 4472 B    |
| Imposter        | 1,709.39 ns   | 3.703 ns     | 3.283 ns     | 11192 B   |
| Mockolate       | 1,096.44 ns   | 15.810 ns    | 14.789 ns    | 5240 B    |
| Moq             | 480,236.40 ns | 2,351.721 ns | 2,199.801 ns | 34699 B   |
| NSubstitute     | 12,563.78 ns  | 58.588 ns    | 51.937 ns    | 16763 B   |
| FakeItEasy      | 13,513.18 ns  | 118.415 ns   | 110.766 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-02T02:49:53.672Z*
