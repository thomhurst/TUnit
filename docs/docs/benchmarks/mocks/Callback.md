---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.9 ns | 9.16 ns | 8.57 ns | 2.98 KB |
| Imposter | 558.3 ns | 7.14 ns | 6.68 ns | 2.66 KB |
| Mockolate | 626.0 ns | 4.82 ns | 4.27 ns | 1.8 KB |
| Moq | 139,895.6 ns | 436.35 ns | 386.81 ns | 13.29 KB |
| NSubstitute | 4,200.5 ns | 13.78 ns | 12.89 ns | 7.93 KB |
| FakeItEasy | 5,319.7 ns | 19.04 ns | 17.81 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 167875
  bar [755.9, 558.3, 626, 139895.6, 4200.5, 5319.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 862.4 ns | 7.15 ns | 6.69 ns | 3.06 KB |
| Imposter | 626.9 ns | 10.44 ns | 9.76 ns | 2.82 KB |
| Mockolate | 749.6 ns | 4.57 ns | 4.27 ns | 2.13 KB |
| Moq | 146,236.5 ns | 1,265.92 ns | 1,122.21 ns | 13.75 KB |
| NSubstitute | 4,833.3 ns | 13.31 ns | 10.39 ns | 8.53 KB |
| FakeItEasy | 6,400.9 ns | 23.85 ns | 21.14 ns | 9.41 KB |

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
  y-axis "Time (ns)" 0 --> 175484
  bar [862.4, 626.9, 749.6, 146236.5, 4833.3, 6400.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-25T03:21:02.718Z*
