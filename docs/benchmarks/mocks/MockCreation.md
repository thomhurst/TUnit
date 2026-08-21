# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev   | Allocated |
| --------------- | ----------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 23.03 ns    | 0.195 ns  | 0.173 ns | 200 B     |
| Imposter        | 78.30 ns    | 0.412 ns  | 0.365 ns | 440 B     |
| Mockolate       | 14.01 ns    | 0.163 ns  | 0.144 ns | 160 B     |
| Moq             | 986.81 ns   | 11.046 ns | 9.224 ns | 2048 B    |
| NSubstitute     | 1,360.48 ns | 7.596 ns  | 7.105 ns | 5000 B    |
| FakeItEasy      | 1,267.14 ns | 10.470 ns | 9.281 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error    | StdDev   | Allocated |
| --------------- | ----------- | -------- | -------- | --------- |
| **TUnit.Mocks** | 23.09 ns    | 0.276 ns | 0.258 ns | 200 B     |
| Imposter        | 138.27 ns   | 1.651 ns | 1.545 ns | 696 B     |
| Mockolate       | 14.05 ns    | 0.101 ns | 0.094 ns | 176 B     |
| Moq             | 927.31 ns   | 3.353 ns | 2.972 ns | 1912 B    |
| NSubstitute     | 1,350.85 ns | 6.298 ns | 5.259 ns | 5000 B    |
| FakeItEasy      | 1,259.15 ns | 8.341 ns | 7.802 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-21T02:46:27.792Z*
