# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 32.88 ns    | 0.415 ns  | 0.389 ns  | 200 B     |
| Imposter        | 100.99 ns   | 0.999 ns  | 0.935 ns  | 440 B     |
| Mockolate       | 21.26 ns    | 0.288 ns  | 0.270 ns  | 160 B     |
| Moq             | 1,409.16 ns | 24.319 ns | 22.748 ns | 2048 B    |
| NSubstitute     | 1,972.85 ns | 9.957 ns  | 9.314 ns  | 5000 B    |
| FakeItEasy      | 1,783.73 ns | 29.642 ns | 27.727 ns | 2714 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 33.46 ns    | 0.494 ns  | 0.438 ns  | 200 B     |
| Imposter        | 152.20 ns   | 2.682 ns  | 2.509 ns  | 696 B     |
| Mockolate       | 19.79 ns    | 0.424 ns  | 0.471 ns  | 176 B     |
| Moq             | 1,538.26 ns | 8.508 ns  | 7.542 ns  | 1912 B    |
| NSubstitute     | 1,988.31 ns | 13.427 ns | 12.560 ns | 5000 B    |
| FakeItEasy      | 1,904.78 ns | 24.725 ns | 23.127 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-13T02:33:28.296Z*
