---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 711.5 ns | 9.34 ns | 8.74 ns | 3.11 KB |
| Imposter | 500.4 ns | 9.87 ns | 17.79 ns | 2.66 KB |
| Mockolate | 365.8 ns | 6.63 ns | 6.20 ns | 1.8 KB |
| Moq | 185,926.6 ns | 910.88 ns | 852.03 ns | 13.14 KB |
| NSubstitute | 4,773.6 ns | 41.75 ns | 39.05 ns | 7.93 KB |
| FakeItEasy | 5,380.3 ns | 51.69 ns | 48.35 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223112
  bar [711.5, 500.4, 365.8, 185926.6, 4773.6, 5380.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 877.9 ns | 14.88 ns | 13.92 ns | 3.2 KB |
| Imposter | 534.5 ns | 5.56 ns | 5.20 ns | 2.82 KB |
| Mockolate | 416.7 ns | 8.21 ns | 7.68 ns | 1.84 KB |
| Moq | 194,751.1 ns | 965.12 ns | 902.77 ns | 13.73 KB |
| NSubstitute | 5,175.6 ns | 48.62 ns | 45.48 ns | 8.53 KB |
| FakeItEasy | 6,529.5 ns | 91.34 ns | 80.97 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 233702
  bar [877.9, 534.5, 416.7, 194751.1, 5175.6, 6529.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-09T03:24:10.827Z*
