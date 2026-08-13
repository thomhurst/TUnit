---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 734.8 ns | 11.19 ns | 9.92 ns | 3.11 KB |
| Imposter | 524.0 ns | 10.30 ns | 11.02 ns | 2.66 KB |
| Mockolate | 394.8 ns | 2.89 ns | 2.70 ns | 1.8 KB |
| Moq | 188,017.9 ns | 833.58 ns | 738.95 ns | 13.26 KB |
| NSubstitute | 5,127.3 ns | 67.20 ns | 62.86 ns | 7.85 KB |
| FakeItEasy | 5,644.8 ns | 72.83 ns | 68.12 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 225622
  bar [734.8, 524, 394.8, 188017.9, 5127.3, 5644.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 911.8 ns | 16.69 ns | 15.61 ns | 3.2 KB |
| Imposter | 631.1 ns | 7.46 ns | 6.98 ns | 2.82 KB |
| Mockolate | 464.8 ns | 8.91 ns | 9.15 ns | 1.84 KB |
| Moq | 195,675.8 ns | 959.29 ns | 897.32 ns | 13.73 KB |
| NSubstitute | 5,721.5 ns | 20.30 ns | 16.95 ns | 8.41 KB |
| FakeItEasy | 6,738.9 ns | 74.01 ns | 61.80 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 234811
  bar [911.8, 631.1, 464.8, 195675.8, 5721.5, 6738.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-13T03:11:34.997Z*
