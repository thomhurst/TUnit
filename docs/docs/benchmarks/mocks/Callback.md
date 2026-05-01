---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 629.0 ns | 3.83 ns | 3.59 ns | 2.98 KB |
| Imposter | 462.0 ns | 1.60 ns | 1.50 ns | 2.66 KB |
| Mockolate | 522.2 ns | 1.45 ns | 1.28 ns | 2.53 KB |
| Moq | 182,807.7 ns | 1,197.44 ns | 1,120.09 ns | 13.14 KB |
| NSubstitute | 4,428.0 ns | 21.94 ns | 20.53 ns | 7.93 KB |
| FakeItEasy | 5,132.9 ns | 33.50 ns | 27.97 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 219370
  bar [629, 462, 522.2, 182807.7, 4428, 5132.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 694.9 ns | 4.92 ns | 4.60 ns | 3.06 KB |
| Imposter | 559.7 ns | 4.40 ns | 4.12 ns | 2.82 KB |
| Mockolate | 556.2 ns | 1.88 ns | 1.76 ns | 2.58 KB |
| Moq | 192,111.4 ns | 465.62 ns | 363.52 ns | 13.73 KB |
| NSubstitute | 5,116.0 ns | 50.46 ns | 42.14 ns | 8.53 KB |
| FakeItEasy | 6,300.7 ns | 88.87 ns | 74.21 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 230534
  bar [694.9, 559.7, 556.2, 192111.4, 5116, 6300.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-01T03:25:57.964Z*
