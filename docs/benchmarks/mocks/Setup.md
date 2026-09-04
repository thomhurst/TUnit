# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 421.5 ns     | 7.07 ns     | 5.90 ns     | 2.34 KB   |
| Imposter        | 665.9 ns     | 13.34 ns    | 25.05 ns    | 6.12 KB   |
| Mockolate       | 255.6 ns     | 4.02 ns     | 3.36 ns     | 1.41 KB   |
| Moq             | 159,727.2 ns | 2,598.80 ns | 2,552.37 ns | 28.61 KB  |
| NSubstitute     | 4,769.0 ns   | 93.76 ns    | 87.71 ns    | 9.01 KB   |
| FakeItEasy      | 4,569.9 ns   | 88.62 ns    | 118.30 ns   | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 674.2 ns    | 12.98 ns  | 16.41 ns  | 3.15 KB   |
| Imposter        | 1,087.0 ns  | 21.54 ns  | 28.75 ns  | 10.59 KB  |
| Mockolate       | 440.9 ns    | 6.52 ns   | 5.78 ns   | 2.35 KB   |
| Moq             | 42,070.2 ns | 453.37 ns | 378.58 ns | 16.52 KB  |
| NSubstitute     | 8,155.3 ns  | 161.65 ns | 315.28 ns | 20.66 KB  |
| FakeItEasy      | 4,233.0 ns  | 83.31 ns  | 129.70 ns | 11.7 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-04T02:33:16.366Z*
