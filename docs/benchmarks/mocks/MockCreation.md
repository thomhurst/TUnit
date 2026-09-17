# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.44 ns    | 0.235 ns  | 0.196 ns  | 200 B     |
| Imposter        | 87.30 ns    | 0.577 ns  | 0.511 ns  | 440 B     |
| Mockolate       | 16.58 ns    | 0.184 ns  | 0.154 ns  | 160 B     |
| Moq             | 1,334.30 ns | 20.457 ns | 19.135 ns | 2048 B    |
| NSubstitute     | 1,880.41 ns | 12.392 ns | 10.985 ns | 5000 B    |
| FakeItEasy      | 1,686.81 ns | 16.989 ns | 15.061 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.55 ns    | 0.186 ns  | 0.155 ns  | 200 B     |
| Imposter        | 136.28 ns   | 1.011 ns  | 0.896 ns  | 696 B     |
| Mockolate       | 17.08 ns    | 0.299 ns  | 0.279 ns  | 176 B     |
| Moq             | 1,290.71 ns | 7.279 ns  | 6.453 ns  | 1912 B    |
| NSubstitute     | 1,871.44 ns | 10.196 ns | 9.537 ns  | 5000 B    |
| FakeItEasy      | 1,776.41 ns | 20.076 ns | 18.779 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-17T02:33:27.459Z*
