# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 688.72 ns     | 2.025 ns   | 1.795 ns   | 3008 B    |
| Imposter        | 675.27 ns     | 4.531 ns   | 4.016 ns   | 4688 B    |
| Mockolate       | 440.48 ns     | 1.777 ns   | 1.484 ns   | 2128 B    |
| Moq             | 350,646.81 ns | 805.187 ns | 672.367 ns | 24325 B   |
| NSubstitute     | 6,898.23 ns   | 20.072 ns  | 15.671 ns  | 10176 B   |
| FakeItEasy      | 7,431.86 ns   | 23.269 ns  | 19.431 ns  | 10722 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 50.51 ns     | 0.704 ns   | 0.659 ns   | 320 B     |
| Imposter        | 310.62 ns    | 2.167 ns   | 2.027 ns   | 2400 B    |
| Mockolate       | 216.70 ns    | 0.555 ns   | 0.520 ns   | 1144 B    |
| Moq             | 89,735.83 ns | 243.859 ns | 190.389 ns | 6918 B    |
| NSubstitute     | 3,891.54 ns  | 14.568 ns  | 12.914 ns  | 7088 B    |
| FakeItEasy      | 3,647.10 ns  | 12.642 ns  | 11.826 ns  | 5210 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 1,289.06 ns   | 4.317 ns     | 3.605 ns     | 4472 B    |
| Imposter        | 1,683.02 ns   | 5.554 ns     | 5.195 ns     | 11192 B   |
| Mockolate       | 1,046.61 ns   | 3.099 ns     | 2.747 ns     | 5240 B    |
| Moq             | 478,707.60 ns | 2,483.719 ns | 2,074.017 ns | 34699 B   |
| NSubstitute     | 12,293.73 ns  | 129.821 ns   | 121.435 ns   | 16929 B   |
| FakeItEasy      | 12,915.06 ns  | 158.250 ns   | 140.285 ns   | 19233 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-14T02:37:23.172Z*
