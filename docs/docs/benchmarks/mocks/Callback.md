---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 678.5 ns | 6.70 ns | 6.27 ns | 3.11 KB |
| Imposter | 517.1 ns | 9.31 ns | 8.26 ns | 2.66 KB |
| Mockolate | 363.2 ns | 4.17 ns | 3.69 ns | 1.8 KB |
| Moq | 186,932.5 ns | 737.46 ns | 615.82 ns | 13.14 KB |
| NSubstitute | 4,506.5 ns | 41.03 ns | 38.38 ns | 7.93 KB |
| FakeItEasy | 5,381.5 ns | 105.74 ns | 133.73 ns | 7.44 KB |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Callback Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 224319
  bar [678.5, 517.1, 363.2, 186932.5, 4506.5, 5381.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 853.3 ns | 17.01 ns | 17.47 ns | 3.2 KB |
| Imposter | 587.2 ns | 11.64 ns | 10.89 ns | 2.82 KB |
| Mockolate | 411.7 ns | 3.88 ns | 3.63 ns | 1.84 KB |
| Moq | 194,507.4 ns | 926.87 ns | 866.99 ns | 13.73 KB |
| NSubstitute | 5,297.6 ns | 102.21 ns | 104.96 ns | 8.53 KB |
| FakeItEasy | 6,755.9 ns | 130.95 ns | 155.89 ns | 9.4 KB |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Callback (with args) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 233409
  bar [853.3, 587.2, 411.7, 194507.4, 5297.6, 6755.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-03T04:04:39.541Z*
