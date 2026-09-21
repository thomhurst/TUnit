# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.76 ns    | 0.427 ns  | 0.379 ns  | 200 B     |
| Imposter        | 100.96 ns   | 0.431 ns  | 0.382 ns  | 440 B     |
| Mockolate       | 23.21 ns    | 0.348 ns  | 0.325 ns  | 160 B     |
| Moq             | 1,446.86 ns | 20.264 ns | 18.955 ns | 2048 B    |
| NSubstitute     | 2,033.47 ns | 22.279 ns | 20.840 ns | 5000 B    |
| FakeItEasy      | 1,872.78 ns | 22.003 ns | 20.581 ns | 2714 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 32.22 ns    | 0.308 ns  | 0.288 ns  | 200 B     |
| Imposter        | 152.60 ns   | 2.246 ns  | 2.101 ns  | 696 B     |
| Mockolate       | 19.23 ns    | 0.336 ns  | 0.298 ns  | 176 B     |
| Moq             | 1,340.21 ns | 8.911 ns  | 8.336 ns  | 1912 B    |
| NSubstitute     | 2,011.38 ns | 16.544 ns | 15.476 ns | 5000 B    |
| FakeItEasy      | 1,838.05 ns | 36.530 ns | 42.068 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-21T02:37:24.392Z*
