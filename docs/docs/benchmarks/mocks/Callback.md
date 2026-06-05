---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 652.9 ns | 2.69 ns | 2.39 ns | 3.1 KB |
| Imposter | 464.2 ns | 1.98 ns | 1.85 ns | 2.66 KB |
| Mockolate | 360.8 ns | 2.27 ns | 2.13 ns | 1.91 KB |
| Moq | 137,030.6 ns | 1,292.00 ns | 1,145.32 ns | 13.29 KB |
| NSubstitute | 4,092.6 ns | 20.38 ns | 19.07 ns | 7.93 KB |
| FakeItEasy | 4,580.3 ns | 17.97 ns | 16.81 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 164437
  bar [652.9, 464.2, 360.8, 137030.6, 4092.6, 4580.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.6 ns | 3.10 ns | 2.42 ns | 3.2 KB |
| Imposter | 537.5 ns | 1.65 ns | 1.55 ns | 2.82 KB |
| Mockolate | 387.1 ns | 1.31 ns | 1.09 ns | 1.95 KB |
| Moq | 141,061.8 ns | 813.55 ns | 679.36 ns | 13.73 KB |
| NSubstitute | 4,635.3 ns | 24.59 ns | 20.53 ns | 8.53 KB |
| FakeItEasy | 5,644.4 ns | 18.61 ns | 16.50 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 169275
  bar [755.6, 537.5, 387.1, 141061.8, 4635.3, 5644.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-05T03:30:04.148Z*
