# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.46 ns    | 0.517 ns  | 0.531 ns  | 200 B     |
| Imposter        | 92.00 ns    | 1.385 ns  | 1.360 ns  | 440 B     |
| Mockolate       | 17.65 ns    | 0.414 ns  | 0.725 ns  | 160 B     |
| Moq             | 1,374.57 ns | 24.875 ns | 23.268 ns | 2048 B    |
| NSubstitute     | 1,841.72 ns | 30.053 ns | 28.111 ns | 5000 B    |
| FakeItEasy      | 1,726.45 ns | 34.336 ns | 46.999 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.50 ns    | 0.481 ns  | 0.426 ns  | 200 B     |
| Imposter        | 141.13 ns   | 2.767 ns  | 2.588 ns  | 696 B     |
| Mockolate       | 16.90 ns    | 0.265 ns  | 0.247 ns  | 176 B     |
| Moq             | 1,373.19 ns | 8.777 ns  | 8.210 ns  | 1912 B    |
| NSubstitute     | 1,859.87 ns | 16.446 ns | 15.384 ns | 5000 B    |
| FakeItEasy      | 1,656.48 ns | 32.617 ns | 33.496 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-12T02:33:29.738Z*
