---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 653.7 ns | 6.72 ns | 6.29 ns | 3.08 KB |
| Imposter | 518.2 ns | 4.67 ns | 4.14 ns | 2.66 KB |
| Mockolate | 356.2 ns | 3.83 ns | 3.59 ns | 1.91 KB |
| Moq | 184,495.9 ns | 1,971.14 ns | 1,747.37 ns | 13.14 KB |
| NSubstitute | 4,669.3 ns | 32.63 ns | 30.53 ns | 7.93 KB |
| FakeItEasy | 5,347.5 ns | 24.57 ns | 21.78 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221396
  bar [653.7, 518.2, 356.2, 184495.9, 4669.3, 5347.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 789.1 ns | 5.07 ns | 4.50 ns | 3.16 KB |
| Imposter | 515.0 ns | 3.30 ns | 2.93 ns | 2.82 KB |
| Mockolate | 394.5 ns | 2.95 ns | 2.61 ns | 1.95 KB |
| Moq | 193,294.2 ns | 660.34 ns | 585.37 ns | 13.73 KB |
| NSubstitute | 5,060.0 ns | 41.87 ns | 37.12 ns | 8.53 KB |
| FakeItEasy | 6,484.5 ns | 123.29 ns | 141.98 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 231954
  bar [789.1, 515, 394.5, 193294.2, 5060, 6484.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-22T03:28:55.311Z*
