# Verification Benchmark

> Mock verification workflows, including creation, setup and invocation where applicable — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock verification workflows, including creation, setup and invocation where applicable:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 505.29 ns     | 6.913 ns     | 5.773 ns     | 1800 B    |
| Imposter        | 281.49 ns     | 4.366 ns     | 4.084 ns     | 1096 B    |
| Mockolate       | 187.29 ns     | 2.114 ns     | 1.977 ns     | 712 B     |
| Moq             | 370,263.90 ns | 2,949.772 ns | 2,614.896 ns | 24300 B   |
| NSubstitute     | 6,810.69 ns   | 82.174 ns    | 76.865 ns    | 10024 B   |
| FakeItEasy      | 7,182.72 ns   | 50.192 ns    | 46.950 ns    | 10506 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 49.79 ns     | 0.596 ns   | 0.497 ns   | 352 B     |
| Imposter        | 125.16 ns    | 2.175 ns   | 2.034 ns   | 616 B     |
| Mockolate       | 67.60 ns     | 0.800 ns   | 0.749 ns   | 368 B     |
| Moq             | 94,746.80 ns | 429.201 ns | 358.402 ns | 6917 B    |
| NSubstitute     | 3,766.00 ns  | 22.469 ns  | 21.018 ns  | 7056 B    |
| FakeItEasy      | 3,544.27 ns  | 36.843 ns  | 32.661 ns  | 5105 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 971.23 ns     | 10.402 ns    | 9.222 ns     | 3240 B    |
| Imposter        | 528.38 ns     | 6.475 ns     | 6.057 ns     | 2288 B    |
| Mockolate       | 421.64 ns     | 5.752 ns     | 5.099 ns     | 1360 B    |
| Moq             | 501,669.26 ns | 2,560.482 ns | 2,395.076 ns | 35225 B   |
| NSubstitute     | 12,515.22 ns  | 75.822 ns    | 67.215 ns    | 16633 B   |
| FakeItEasy      | 12,633.91 ns  | 109.456 ns   | 102.385 ns   | 18783 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for mock verification workflows, including creation, setup and invocation where applicable.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-21T02:20:26.016Z*
