# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 569.9 ns     | 3.90 ns   | 3.26 ns   | 2.34 KB   |
| Imposter        | 789.5 ns     | 10.03 ns  | 9.38 ns   | 6.12 KB   |
| Mockolate       | 338.5 ns     | 3.49 ns   | 3.26 ns   | 1.41 KB   |
| Moq             | 295,632.3 ns | 970.76 ns | 757.91 ns | 28.52 KB  |
| NSubstitute     | 5,964.2 ns   | 32.16 ns  | 26.85 ns  | 9.01 KB   |
| FakeItEasy      | 7,147.0 ns   | 44.43 ns  | 37.10 ns  | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error       | StdDev      | Allocated |
| --------------- | ----------- | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 828.7 ns    | 4.91 ns     | 4.59 ns     | 3.15 KB   |
| Imposter        | 1,392.5 ns  | 8.67 ns     | 7.69 ns     | 10.59 KB  |
| Mockolate       | 585.2 ns    | 4.76 ns     | 4.45 ns     | 2.35 KB   |
| Moq             | 89,520.2 ns | 1,097.27 ns | 1,026.39 ns | 16.61 KB  |
| NSubstitute     | 11,429.2 ns | 103.55 ns   | 96.86 ns    | 20.31 KB  |
| FakeItEasy      | 7,317.7 ns  | 81.40 ns    | 76.15 ns    | 11.82 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-24T02:46:06.016Z*
