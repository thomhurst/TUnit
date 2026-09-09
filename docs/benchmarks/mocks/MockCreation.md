# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 32.69 ns    | 0.715 ns  | 0.852 ns  | 200 B     |
| Imposter        | 94.68 ns    | 1.950 ns  | 2.467 ns  | 440 B     |
| Mockolate       | 17.52 ns    | 0.405 ns  | 0.378 ns  | 160 B     |
| Moq             | 1,472.45 ns | 22.750 ns | 21.280 ns | 2048 B    |
| NSubstitute     | 1,879.73 ns | 34.125 ns | 31.920 ns | 5000 B    |
| FakeItEasy      | 1,753.38 ns | 19.642 ns | 17.412 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.02 ns    | 0.632 ns  | 0.591 ns  | 200 B     |
| Imposter        | 147.36 ns   | 1.759 ns  | 1.559 ns  | 696 B     |
| Mockolate       | 18.65 ns    | 0.335 ns  | 0.314 ns  | 176 B     |
| Moq             | 1,373.49 ns | 8.376 ns  | 7.835 ns  | 1912 B    |
| NSubstitute     | 2,012.19 ns | 39.091 ns | 38.393 ns | 5000 B    |
| FakeItEasy      | 1,782.88 ns | 34.643 ns | 45.046 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-09T02:32:56.707Z*
