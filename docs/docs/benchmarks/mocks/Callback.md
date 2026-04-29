---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 655.8 ns | 9.16 ns | 8.57 ns | 2.98 KB |
| Imposter | 489.3 ns | 9.45 ns | 7.89 ns | 2.66 KB |
| Mockolate | 542.6 ns | 7.18 ns | 5.99 ns | 1.8 KB |
| Moq | 185,895.2 ns | 653.58 ns | 611.36 ns | 13.26 KB |
| NSubstitute | 4,557.5 ns | 19.38 ns | 16.18 ns | 7.93 KB |
| FakeItEasy | 5,235.3 ns | 31.02 ns | 29.01 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223075
  bar [655.8, 489.3, 542.6, 185895.2, 4557.5, 5235.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 770.3 ns | 14.04 ns | 12.44 ns | 3.06 KB |
| Imposter | 537.1 ns | 4.81 ns | 4.50 ns | 2.82 KB |
| Mockolate | 680.6 ns | 8.20 ns | 7.67 ns | 2.13 KB |
| Moq | 190,570.9 ns | 1,298.06 ns | 1,150.70 ns | 13.73 KB |
| NSubstitute | 5,171.6 ns | 56.47 ns | 50.06 ns | 8.53 KB |
| FakeItEasy | 6,416.3 ns | 60.21 ns | 50.28 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 228686
  bar [770.3, 537.1, 680.6, 190570.9, 5171.6, 6416.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-29T03:24:49.990Z*
