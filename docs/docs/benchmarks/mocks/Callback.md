---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 746.9 ns | 14.95 ns | 19.95 ns | 3.72 KB |
| Imposter | 448.2 ns | 3.22 ns | 3.02 ns | 2.66 KB |
| Mockolate | 496.4 ns | 5.18 ns | 4.85 ns | 1.84 KB |
| Moq | 181,713.4 ns | 1,311.35 ns | 1,226.64 ns | 13.14 KB |
| NSubstitute | 4,340.0 ns | 41.71 ns | 39.02 ns | 7.93 KB |
| FakeItEasy | 5,064.1 ns | 37.17 ns | 32.95 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218057
  bar [746.9, 448.2, 496.4, 181713.4, 4340, 5064.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 824.3 ns | 7.26 ns | 6.79 ns | 3.8 KB |
| Imposter | 539.4 ns | 5.19 ns | 4.86 ns | 2.82 KB |
| Mockolate | 634.0 ns | 3.00 ns | 2.50 ns | 2.22 KB |
| Moq | 190,538.4 ns | 1,553.01 ns | 1,452.68 ns | 13.85 KB |
| NSubstitute | 4,932.8 ns | 46.54 ns | 43.54 ns | 8.53 KB |
| FakeItEasy | 5,989.3 ns | 77.15 ns | 72.17 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 228647
  bar [824.3, 539.4, 634, 190538.4, 4932.8, 5989.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T03:24:56.545Z*
