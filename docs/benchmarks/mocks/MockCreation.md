# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean       | Error      | StdDev     | Allocated |
| --------------- | ---------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 18.132 ns  | 0.1603 ns  | 0.1339 ns  | 200 B     |
| Imposter        | 65.742 ns  | 2.1872 ns  | 6.4490 ns  | 440 B     |
| Mockolate       | 9.959 ns   | 0.2304 ns  | 0.2561 ns  | 160 B     |
| Moq             | 764.295 ns | 14.5472 ns | 12.8957 ns | 2048 B    |
| NSubstitute     | 993.670 ns | 15.1693 ns | 13.4472 ns | 5000 B    |
| FakeItEasy      | 958.064 ns | 17.4188 ns | 30.5076 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 17.239 ns    | 0.3870 ns  | 0.3801 ns  | 200 B     |
| Imposter        | 96.826 ns    | 0.8207 ns  | 0.7276 ns  | 696 B     |
| Mockolate       | 10.583 ns    | 0.2377 ns  | 0.2107 ns  | 176 B     |
| Moq             | 774.138 ns   | 14.7618 ns | 13.0859 ns | 1912 B    |
| NSubstitute     | 1,041.628 ns | 20.0502 ns | 20.5901 ns | 5000 B    |
| FakeItEasy      | 1,003.743 ns | 19.3129 ns | 33.3138 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-05T02:45:14.260Z*
