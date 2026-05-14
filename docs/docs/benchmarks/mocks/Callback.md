---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 635.3 ns | 7.53 ns | 6.67 ns | 2.98 KB |
| Imposter | 486.4 ns | 3.98 ns | 3.32 ns | 2.66 KB |
| Mockolate | 359.7 ns | 3.78 ns | 3.35 ns | 1.91 KB |
| Moq | 137,250.1 ns | 1,388.57 ns | 1,230.93 ns | 13.24 KB |
| NSubstitute | 4,395.7 ns | 24.11 ns | 22.55 ns | 7.93 KB |
| FakeItEasy | 4,737.5 ns | 73.89 ns | 69.11 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 164701
  bar [635.3, 486.4, 359.7, 137250.1, 4395.7, 4737.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 784.9 ns | 8.75 ns | 8.18 ns | 3.06 KB |
| Imposter | 565.1 ns | 5.54 ns | 4.63 ns | 2.82 KB |
| Mockolate | 404.4 ns | 3.86 ns | 3.43 ns | 1.95 KB |
| Moq | 143,608.6 ns | 876.32 ns | 684.17 ns | 13.73 KB |
| NSubstitute | 4,832.2 ns | 53.44 ns | 47.37 ns | 8.53 KB |
| FakeItEasy | 5,756.5 ns | 29.21 ns | 22.81 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 172331
  bar [784.9, 565.1, 404.4, 143608.6, 4832.2, 5756.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-14T03:27:14.658Z*
