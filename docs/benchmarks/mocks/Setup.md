# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 554.8 ns     | 7.61 ns     | 7.12 ns     | 2.37 KB   |
| Imposter        | 806.6 ns     | 10.60 ns    | 8.85 ns     | 6.09 KB   |
| Mockolate       | 322.1 ns     | 2.52 ns     | 2.24 ns     | 1.41 KB   |
| Moq             | 435,489.9 ns | 1,342.70 ns | 1,190.26 ns | 28.64 KB  |
| NSubstitute     | 6,469.0 ns   | 27.78 ns    | 25.99 ns    | 9.01 KB   |
| FakeItEasy      | 8,700.2 ns   | 56.77 ns    | 50.33 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 800.4 ns     | 11.83 ns  | 10.49 ns  | 3.18 KB   |
| Imposter        | 1,428.8 ns   | 24.73 ns  | 23.13 ns  | 10.55 KB  |
| Mockolate       | 558.5 ns     | 6.64 ns   | 6.21 ns   | 2.35 KB   |
| Moq             | 116,393.9 ns | 700.43 ns | 655.18 ns | 16.35 KB  |
| NSubstitute     | 13,223.4 ns  | 139.50 ns | 130.49 ns | 20.31 KB  |
| FakeItEasy      | 7,989.4 ns   | 111.02 ns | 92.70 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-10T02:40:36.038Z*
