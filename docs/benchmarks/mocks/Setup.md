# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 536.0 ns     | 10.73 ns    | 18.50 ns    | 2.37 KB   |
| Imposter        | 773.1 ns     | 15.29 ns    | 36.64 ns    | 6.12 KB   |
| Mockolate       | 306.7 ns     | 6.02 ns     | 6.18 ns     | 1.41 KB   |
| Moq             | 164,633.8 ns | 3,289.03 ns | 4,717.02 ns | 28.54 KB  |
| NSubstitute     | 4,793.5 ns   | 33.00 ns    | 29.25 ns    | 9.06 KB   |
| FakeItEasy      | 4,873.2 ns   | 40.88 ns    | 34.14 ns    | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev      | Allocated |
| --------------- | ----------- | --------- | ----------- | --------- |
| **TUnit.Mocks** | 755.0 ns    | 12.06 ns  | 10.69 ns    | 3.18 KB   |
| Imposter        | 1,459.6 ns  | 49.42 ns  | 145.72 ns   | 10.59 KB  |
| Mockolate       | 544.2 ns    | 10.85 ns  | 12.50 ns    | 2.35 KB   |
| Moq             | 46,285.8 ns | 920.11 ns | 2,168.80 ns | 16.33 KB  |
| NSubstitute     | 8,741.8 ns  | 63.68 ns  | 53.18 ns    | 20.47 KB  |
| FakeItEasy      | 4,836.0 ns  | 80.57 ns  | 89.55 ns    | 11.7 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-05T02:45:14.260Z*
