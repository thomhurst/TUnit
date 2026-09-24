# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.15 ns    | 0.370 ns  | 0.346 ns  | 200 B     |
| Imposter        | 93.10 ns    | 1.876 ns  | 2.568 ns  | 440 B     |
| Mockolate       | 17.21 ns    | 0.402 ns  | 0.892 ns  | 160 B     |
| Moq             | 1,331.11 ns | 20.203 ns | 18.898 ns | 2048 B    |
| NSubstitute     | 1,850.97 ns | 26.023 ns | 24.342 ns | 5000 B    |
| FakeItEasy      | 1,688.90 ns | 32.257 ns | 37.147 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.09 ns    | 0.210 ns  | 0.197 ns  | 200 B     |
| Imposter        | 139.28 ns   | 2.627 ns  | 2.811 ns  | 696 B     |
| Mockolate       | 17.17 ns    | 0.296 ns  | 0.277 ns  | 176 B     |
| Moq             | 1,401.97 ns | 23.530 ns | 20.859 ns | 1912 B    |
| NSubstitute     | 1,864.93 ns | 11.376 ns | 10.641 ns | 5000 B    |
| FakeItEasy      | 1,659.61 ns | 14.174 ns | 12.565 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-24T02:33:35.117Z*
