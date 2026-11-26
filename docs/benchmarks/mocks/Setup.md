# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 330.7 ns     | 1.59 ns   | 1.49 ns   | 1264 B    |
| Imposter        | 377.8 ns     | 2.73 ns   | 2.42 ns   | 1360 B    |
| Mockolate       | 215.7 ns     | 0.82 ns   | 0.77 ns   | 608 B     |
| Moq             | 227,676.7 ns | 843.42 ns | 747.67 ns | 29341 B   |
| NSubstitute     | 6,150.4 ns   | 26.07 ns  | 23.11 ns  | 9184 B    |
| FakeItEasy      | 6,052.3 ns   | 39.29 ns  | 36.76 ns  | 10381 B   |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 552.9 ns    | 7.47 ns   | 6.62 ns   | 2032 B    |
| Imposter        | 687.0 ns    | 3.60 ns   | 3.37 ns   | 2536 B    |
| Mockolate       | 363.0 ns    | 2.42 ns   | 2.14 ns   | 1032 B    |
| Moq             | 57,519.0 ns | 285.44 ns | 253.04 ns | 16383 B   |
| NSubstitute     | 10,356.9 ns | 38.35 ns  | 32.02 ns  | 20649 B   |
| FakeItEasy      | 6,078.3 ns  | 48.27 ns  | 45.15 ns  | 11632 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-26T02:21:09.958Z*
