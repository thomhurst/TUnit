# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.17 ns    | 0.566 ns  | 0.530 ns  | 200 B     |
| Imposter        | 101.17 ns   | 0.677 ns  | 0.633 ns  | 440 B     |
| Mockolate       | 18.90 ns    | 0.103 ns  | 0.086 ns  | 160 B     |
| Moq             | 1,270.50 ns | 21.487 ns | 20.099 ns | 2048 B    |
| NSubstitute     | 1,803.94 ns | 25.620 ns | 23.965 ns | 5000 B    |
| FakeItEasy      | 1,807.54 ns | 35.821 ns | 59.849 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.40 ns    | 0.258 ns  | 0.241 ns  | 200 B     |
| Imposter        | 157.65 ns   | 0.838 ns  | 0.784 ns  | 696 B     |
| Mockolate       | 18.20 ns    | 0.168 ns  | 0.140 ns  | 176 B     |
| Moq             | 1,266.11 ns | 10.725 ns | 10.032 ns | 1912 B    |
| NSubstitute     | 1,895.33 ns | 17.260 ns | 16.145 ns | 5000 B    |
| FakeItEasy      | 1,889.94 ns | 36.180 ns | 40.214 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-03T02:45:05.205Z*
