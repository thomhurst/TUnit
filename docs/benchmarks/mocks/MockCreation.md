# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 35.23 ns    | 0.788 ns  | 2.102 ns  | 200 B     |
| Imposter        | 105.43 ns   | 2.189 ns  | 4.058 ns  | 440 B     |
| Mockolate       | 23.03 ns    | 0.706 ns  | 2.081 ns  | 160 B     |
| Moq             | 1,357.98 ns | 25.478 ns | 25.023 ns | 2048 B    |
| NSubstitute     | 1,895.78 ns | 37.093 ns | 36.431 ns | 5000 B    |
| FakeItEasy      | 1,926.44 ns | 37.416 ns | 43.089 ns | 2723 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 34.21 ns    | 0.803 ns  | 2.355 ns  | 200 B     |
| Imposter        | 169.80 ns   | 3.490 ns  | 8.626 ns  | 696 B     |
| Mockolate       | 22.34 ns    | 0.631 ns  | 1.840 ns  | 176 B     |
| Moq             | 1,459.49 ns | 9.258 ns  | 8.207 ns  | 1912 B    |
| NSubstitute     | 2,165.10 ns | 25.549 ns | 23.899 ns | 5000 B    |
| FakeItEasy      | 1,913.10 ns | 22.821 ns | 21.347 ns | 2723 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-28T05:02:48.374Z*
