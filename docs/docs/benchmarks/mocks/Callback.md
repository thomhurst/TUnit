---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 390.9 ns | 7.79 ns | 17.43 ns | 3200 B |
| Imposter | 156.6 ns | 3.11 ns | 3.05 ns | 848 B |
| Mockolate | 207.0 ns | 3.99 ns | 5.72 ns | 1824 B |
| Moq | 58,903.4 ns | 1,138.57 ns | 1,311.18 ns | 13577 B |
| NSubstitute | 2,522.8 ns | 49.34 ns | 58.74 ns | 8008 B |
| FakeItEasy | 2,559.4 ns | 50.77 ns | 104.85 ns | 7549 B |

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
  y-axis "Time (ns)" 0 --> 70685
  bar [390.9, 156.6, 207, 58903.4, 2522.8, 2559.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 475.3 ns | 8.42 ns | 16.01 ns | 3296 B |
| Imposter | 170.9 ns | 2.78 ns | 2.60 ns | 896 B |
| Mockolate | 239.2 ns | 4.05 ns | 3.59 ns | 1872 B |
| Moq | 61,257.2 ns | 661.80 ns | 619.05 ns | 13989 B |
| NSubstitute | 2,677.5 ns | 52.52 ns | 84.82 ns | 8584 B |
| FakeItEasy | 3,063.4 ns | 58.70 ns | 65.25 ns | 9624 B |

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
  y-axis "Time (ns)" 0 --> 73509
  bar [475.3, 170.9, 239.2, 61257.2, 2677.5, 3063.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-16T02:25:25.447Z*
