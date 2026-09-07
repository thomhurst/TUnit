# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 566.8 ns     | 9.71 ns     | 9.09 ns     | 2.34 KB   |
| Imposter        | 849.6 ns     | 11.70 ns    | 9.77 ns     | 6.12 KB   |
| Mockolate       | 315.0 ns     | 3.55 ns     | 3.32 ns     | 1.41 KB   |
| Moq             | 425,527.1 ns | 1,603.68 ns | 1,421.62 ns | 28.6 KB   |
| NSubstitute     | 6,262.5 ns   | 29.63 ns    | 26.27 ns    | 9.06 KB   |
| FakeItEasy      | 8,567.4 ns   | 99.84 ns    | 88.50 ns    | 10.6 KB   |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 791.6 ns     | 3.89 ns   | 3.45 ns   | 3.15 KB   |
| Imposter        | 1,437.7 ns   | 10.89 ns  | 10.19 ns  | 10.59 KB  |
| Mockolate       | 550.1 ns     | 3.42 ns   | 3.03 ns   | 2.35 KB   |
| Moq             | 114,108.4 ns | 586.54 ns | 489.79 ns | 16.53 KB  |
| NSubstitute     | 12,522.7 ns  | 103.70 ns | 86.60 ns  | 20.31 KB  |
| FakeItEasy      | 8,066.5 ns   | 61.42 ns  | 57.46 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-07T02:34:20.667Z*
