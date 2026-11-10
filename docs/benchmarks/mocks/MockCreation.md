# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.403

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.05 ns    | 0.663 ns  | 0.908 ns  | 216 B     |
| Imposter        | 74.77 ns    | 1.500 ns  | 2.706 ns  | 360 B     |
| Mockolate       | 17.21 ns    | 0.194 ns  | 0.181 ns  | 160 B     |
| Moq             | 1,316.29 ns | 18.768 ns | 17.556 ns | 2048 B    |
| NSubstitute     | 1,926.32 ns | 22.087 ns | 18.444 ns | 5000 B    |
| FakeItEasy      | 1,861.60 ns | 36.810 ns | 37.802 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.34 ns    | 0.610 ns  | 0.626 ns  | 216 B     |
| Imposter        | 122.13 ns   | 2.530 ns  | 5.393 ns  | 568 B     |
| Mockolate       | 20.90 ns    | 0.464 ns  | 0.477 ns  | 176 B     |
| Moq             | 1,423.72 ns | 9.611 ns  | 8.520 ns  | 1912 B    |
| NSubstitute     | 2,089.91 ns | 41.750 ns | 44.672 ns | 5000 B    |
| FakeItEasy      | 1,869.28 ns | 21.032 ns | 17.563 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-10T02:26:34.876Z*
