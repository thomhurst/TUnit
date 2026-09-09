# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 507.1 ns     | 1.45 ns     | 1.21 ns     | 2.34 KB   |
| Imposter        | 786.7 ns     | 3.85 ns     | 3.41 ns     | 6.12 KB   |
| Mockolate       | 298.4 ns     | 1.65 ns     | 1.54 ns     | 1.41 KB   |
| Moq             | 433,702.9 ns | 1,551.69 ns | 1,451.45 ns | 28.67 KB  |
| NSubstitute     | 6,057.1 ns   | 17.97 ns    | 15.93 ns    | 9.06 KB   |
| FakeItEasy      | 8,423.1 ns   | 20.84 ns    | 18.48 ns    | 10.56 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 745.2 ns     | 7.59 ns     | 7.10 ns     | 3.15 KB   |
| Imposter        | 1,320.4 ns   | 11.31 ns    | 10.58 ns    | 10.59 KB  |
| Mockolate       | 523.3 ns     | 1.78 ns     | 1.49 ns     | 2.35 KB   |
| Moq             | 115,726.9 ns | 1,245.23 ns | 1,164.79 ns | 16.53 KB  |
| NSubstitute     | 12,755.5 ns  | 72.49 ns    | 64.26 ns    | 20.5 KB   |
| FakeItEasy      | 7,401.4 ns   | 68.43 ns    | 60.66 ns    | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-09T02:32:56.707Z*
