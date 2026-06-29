---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 780.3 ns | 7.70 ns | 6.83 ns | 3.11 KB |
| Imposter | 553.7 ns | 4.73 ns | 4.42 ns | 2.66 KB |
| Mockolate | 358.1 ns | 6.68 ns | 5.92 ns | 1.8 KB |
| Moq | 191,884.2 ns | 2,135.73 ns | 1,997.76 ns | 13.14 KB |
| NSubstitute | 4,665.0 ns | 90.49 ns | 84.64 ns | 7.93 KB |
| FakeItEasy | 5,257.2 ns | 40.98 ns | 38.33 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 230262
  bar [780.3, 553.7, 358.1, 191884.2, 4665, 5257.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 852.0 ns | 2.59 ns | 2.30 ns | 3.2 KB |
| Imposter | 556.2 ns | 9.32 ns | 17.51 ns | 2.82 KB |
| Mockolate | 420.5 ns | 4.97 ns | 3.88 ns | 1.84 KB |
| Moq | 196,791.3 ns | 1,092.49 ns | 1,021.91 ns | 13.73 KB |
| NSubstitute | 5,384.9 ns | 76.96 ns | 71.99 ns | 8.53 KB |
| FakeItEasy | 6,716.2 ns | 74.89 ns | 70.05 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 236150
  bar [852, 556.2, 420.5, 196791.3, 5384.9, 6716.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-29T03:30:39.957Z*
