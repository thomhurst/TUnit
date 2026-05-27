---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 641.6 ns | 3.57 ns | 2.98 ns | 3.08 KB |
| Imposter | 473.3 ns | 4.44 ns | 4.16 ns | 2.66 KB |
| Mockolate | 345.9 ns | 1.45 ns | 1.36 ns | 1.91 KB |
| Moq | 134,443.3 ns | 671.84 ns | 561.02 ns | 13.15 KB |
| NSubstitute | 4,139.0 ns | 63.70 ns | 59.59 ns | 7.93 KB |
| FakeItEasy | 4,535.8 ns | 27.28 ns | 22.78 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 161332
  bar [641.6, 473.3, 345.9, 134443.3, 4139, 4535.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 859.4 ns | 6.15 ns | 5.45 ns | 3.16 KB |
| Imposter | 533.2 ns | 2.53 ns | 2.11 ns | 2.82 KB |
| Mockolate | 387.6 ns | 1.79 ns | 1.59 ns | 1.95 KB |
| Moq | 146,925.2 ns | 1,893.35 ns | 1,771.04 ns | 13.73 KB |
| NSubstitute | 4,704.6 ns | 48.45 ns | 45.32 ns | 8.53 KB |
| FakeItEasy | 5,463.5 ns | 36.45 ns | 32.32 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 176311
  bar [859.4, 533.2, 387.6, 146925.2, 4704.6, 5463.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-27T03:29:35.677Z*
