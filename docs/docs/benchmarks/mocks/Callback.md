---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 664.8 ns | 4.28 ns | 3.80 ns | 3.11 KB |
| Imposter | 464.4 ns | 6.75 ns | 5.98 ns | 2.66 KB |
| Mockolate | 365.7 ns | 2.18 ns | 1.93 ns | 1.8 KB |
| Moq | 186,753.3 ns | 2,521.37 ns | 2,235.13 ns | 13.14 KB |
| NSubstitute | 4,642.4 ns | 41.05 ns | 38.40 ns | 7.93 KB |
| FakeItEasy | 5,279.8 ns | 95.16 ns | 84.36 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 224104
  bar [664.8, 464.4, 365.7, 186753.3, 4642.4, 5279.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 862.7 ns | 6.95 ns | 6.16 ns | 3.2 KB |
| Imposter | 541.3 ns | 4.21 ns | 3.74 ns | 2.82 KB |
| Mockolate | 401.8 ns | 3.51 ns | 3.28 ns | 1.84 KB |
| Moq | 191,307.5 ns | 985.84 ns | 823.22 ns | 13.73 KB |
| NSubstitute | 5,018.4 ns | 33.00 ns | 29.25 ns | 8.53 KB |
| FakeItEasy | 6,494.2 ns | 69.60 ns | 61.70 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 229569
  bar [862.7, 541.3, 401.8, 191307.5, 5018.4, 6494.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-30T03:28:32.223Z*
