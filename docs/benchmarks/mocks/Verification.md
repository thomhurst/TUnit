# Verification Benchmark

> Mock verification workflows, including creation, setup and invocation where applicable — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock verification workflows, including creation, setup and invocation where applicable:

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 328.30 ns     | 2.447 ns     | 2.289 ns     | 1512 B    |
| Imposter        | 223.77 ns     | 4.252 ns     | 3.977 ns     | 1096 B    |
| Mockolate       | 152.44 ns     | 2.939 ns     | 2.887 ns     | 712 B     |
| Moq             | 209,970.54 ns | 1,498.224 ns | 1,401.440 ns | 24316 B   |
| NSubstitute     | 5,230.77 ns   | 63.796 ns    | 56.554 ns    | 10024 B   |
| FakeItEasy      | 5,142.18 ns   | 72.457 ns    | 64.231 ns    | 10506 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 43.53 ns     | 0.607 ns   | 0.567 ns   | 360 B     |
| Imposter        | 122.24 ns    | 2.351 ns   | 3.139 ns   | 616 B     |
| Mockolate       | 60.93 ns     | 0.892 ns   | 0.835 ns   | 368 B     |
| Moq             | 54,555.70 ns | 645.506 ns | 572.224 ns | 6917 B    |
| NSubstitute     | 2,994.42 ns  | 48.818 ns  | 45.664 ns  | 7056 B    |
| FakeItEasy      | 2,707.24 ns  | 29.026 ns  | 27.150 ns  | 5105 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 626.79 ns     | 10.495 ns    | 8.764 ns     | 2528 B    |
| Imposter        | 482.69 ns     | 4.335 ns     | 4.055 ns     | 2288 B    |
| Mockolate       | 322.26 ns     | 3.620 ns     | 3.023 ns     | 1360 B    |
| Moq             | 295,703.24 ns | 2,429.323 ns | 2,153.531 ns | 34890 B   |
| NSubstitute     | 9,160.00 ns   | 122.681 ns   | 114.755 ns   | 16633 B   |
| FakeItEasy      | 9,252.41 ns   | 109.102 ns   | 96.716 ns    | 18703 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for mock verification workflows, including creation, setup and invocation where applicable.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-23T02:22:45.286Z*
