# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.18 ns    | 0.275 ns  | 0.257 ns  | 200 B     |
| Imposter        | 86.28 ns    | 0.395 ns  | 0.370 ns  | 440 B     |
| Mockolate       | 16.37 ns    | 0.097 ns  | 0.091 ns  | 160 B     |
| Moq             | 1,271.08 ns | 16.037 ns | 15.001 ns | 2048 B    |
| NSubstitute     | 1,719.32 ns | 11.138 ns | 9.873 ns  | 5000 B    |
| FakeItEasy      | 1,607.13 ns | 21.650 ns | 19.192 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.34 ns    | 0.326 ns  | 0.305 ns  | 200 B     |
| Imposter        | 134.26 ns   | 2.291 ns  | 2.031 ns  | 696 B     |
| Mockolate       | 16.98 ns    | 0.280 ns  | 0.218 ns  | 176 B     |
| Moq             | 1,329.92 ns | 13.974 ns | 13.071 ns | 1912 B    |
| NSubstitute     | 1,780.26 ns | 18.731 ns | 15.641 ns | 5000 B    |
| FakeItEasy      | 1,643.00 ns | 18.592 ns | 16.481 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T03:10:54.658Z*
