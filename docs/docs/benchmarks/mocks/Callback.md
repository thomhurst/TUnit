---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 632.1 ns | 5.31 ns | 4.43 ns | 2.98 KB |
| Imposter | 470.8 ns | 6.79 ns | 6.02 ns | 2.66 KB |
| Mockolate | 352.7 ns | 2.50 ns | 2.34 ns | 1.89 KB |
| Moq | 133,091.6 ns | 1,278.07 ns | 1,132.98 ns | 13.29 KB |
| NSubstitute | 4,171.9 ns | 72.15 ns | 67.49 ns | 7.93 KB |
| FakeItEasy | 4,812.0 ns | 95.87 ns | 114.13 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 159710
  bar [632.1, 470.8, 352.7, 133091.6, 4171.9, 4812]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 710.4 ns | 6.88 ns | 6.43 ns | 3.06 KB |
| Imposter | 553.8 ns | 8.10 ns | 7.57 ns | 2.82 KB |
| Mockolate | 401.8 ns | 1.98 ns | 1.65 ns | 1.94 KB |
| Moq | 141,779.4 ns | 852.90 ns | 797.80 ns | 13.84 KB |
| NSubstitute | 4,804.9 ns | 76.36 ns | 71.43 ns | 8.53 KB |
| FakeItEasy | 5,540.7 ns | 31.53 ns | 29.49 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 170136
  bar [710.4, 553.8, 401.8, 141779.4, 4804.9, 5540.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-04T03:27:14.154Z*
