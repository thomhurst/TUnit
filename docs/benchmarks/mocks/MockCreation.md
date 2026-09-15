# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.22 ns    | 0.468 ns  | 0.539 ns  | 200 B     |
| Imposter        | 68.93 ns    | 0.735 ns  | 0.687 ns  | 440 B     |
| Mockolate       | 13.55 ns    | 0.237 ns  | 0.222 ns  | 160 B     |
| Moq             | 787.34 ns   | 15.268 ns | 21.403 ns | 2048 B    |
| NSubstitute     | 1,254.31 ns | 23.060 ns | 20.442 ns | 5000 B    |
| FakeItEasy      | 972.25 ns   | 17.191 ns | 14.355 ns | 2709 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.56 ns    | 0.407 ns  | 0.340 ns  | 200 B     |
| Imposter        | 128.93 ns   | 2.303 ns  | 2.261 ns  | 696 B     |
| Mockolate       | 17.30 ns    | 0.388 ns  | 0.757 ns  | 176 B     |
| Moq             | 814.26 ns   | 16.164 ns | 27.882 ns | 1912 B    |
| NSubstitute     | 1,287.40 ns | 24.050 ns | 40.183 ns | 5000 B    |
| FakeItEasy      | 1,008.98 ns | 15.700 ns | 15.420 ns | 2709 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-15T02:33:23.208Z*
