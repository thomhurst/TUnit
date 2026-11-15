# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 638.3 ns     | 12.70 ns    | 13.59 ns    | 2.38 KB   |
| Imposter        | 370.8 ns     | 3.11 ns     | 2.91 ns     | 1.33 KB   |
| Mockolate       | 375.9 ns     | 7.30 ns     | 7.49 ns     | 1.39 KB   |
| Moq             | 222,438.9 ns | 1,200.60 ns | 1,002.55 ns | 28.5 KB   |
| NSubstitute     | 6,137.7 ns   | 23.09 ns    | 20.47 ns    | 9.02 KB   |
| FakeItEasy      | 5,904.9 ns   | 32.01 ns    | 26.73 ns    | 10.14 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 956.4 ns    | 5.10 ns   | 4.26 ns   | 3.2 KB    |
| Imposter        | 673.3 ns    | 2.52 ns   | 2.23 ns   | 2.48 KB   |
| Mockolate       | 610.6 ns    | 3.98 ns   | 3.72 ns   | 2.31 KB   |
| Moq             | 55,743.3 ns | 292.87 ns | 259.62 ns | 15.89 KB  |
| NSubstitute     | 10,401.2 ns | 53.48 ns  | 50.03 ns  | 20.49 KB  |
| FakeItEasy      | 5,832.0 ns  | 34.11 ns  | 26.63 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-15T02:24:17.223Z*
