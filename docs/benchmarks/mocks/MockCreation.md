# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 23.81 ns    | 0.137 ns  | 0.121 ns  | 200 B     |
| Imposter        | 79.31 ns    | 0.257 ns  | 0.228 ns  | 440 B     |
| Mockolate       | 13.96 ns    | 0.046 ns  | 0.038 ns  | 160 B     |
| Moq             | 1,034.52 ns | 18.127 ns | 20.148 ns | 2048 B    |
| NSubstitute     | 1,490.67 ns | 15.450 ns | 13.696 ns | 5000 B    |
| FakeItEasy      | 1,469.84 ns | 28.401 ns | 29.166 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 23.42 ns    | 0.049 ns  | 0.046 ns  | 200 B     |
| Imposter        | 125.04 ns   | 0.330 ns  | 0.293 ns  | 696 B     |
| Mockolate       | 15.10 ns    | 0.220 ns  | 0.206 ns  | 176 B     |
| Moq             | 976.43 ns   | 9.204 ns  | 8.610 ns  | 1912 B    |
| NSubstitute     | 1,475.17 ns | 12.125 ns | 11.342 ns | 5000 B    |
| FakeItEasy      | 1,479.55 ns | 27.337 ns | 26.849 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-23T02:45:27.613Z*
