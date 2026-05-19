---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 595.3 ns | 1.51 ns | 1.26 ns | 2.98 KB |
| Imposter | 456.7 ns | 1.95 ns | 1.73 ns | 2.66 KB |
| Mockolate | 343.6 ns | 1.13 ns | 1.06 ns | 1.91 KB |
| Moq | 134,185.1 ns | 1,072.18 ns | 950.46 ns | 13.14 KB |
| NSubstitute | 4,037.1 ns | 20.33 ns | 16.98 ns | 7.93 KB |
| FakeItEasy | 4,430.2 ns | 40.26 ns | 33.62 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 161023
  bar [595.3, 456.7, 343.6, 134185.1, 4037.1, 4430.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 722.4 ns | 2.42 ns | 2.14 ns | 3.06 KB |
| Imposter | 529.3 ns | 0.90 ns | 0.75 ns | 2.82 KB |
| Mockolate | 386.4 ns | 1.07 ns | 0.95 ns | 1.95 KB |
| Moq | 140,927.0 ns | 1,443.89 ns | 1,350.62 ns | 13.73 KB |
| NSubstitute | 4,668.3 ns | 9.18 ns | 7.67 ns | 8.53 KB |
| FakeItEasy | 5,463.7 ns | 29.91 ns | 27.98 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 169113
  bar [722.4, 529.3, 386.4, 140927, 4668.3, 5463.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-19T03:26:57.825Z*
