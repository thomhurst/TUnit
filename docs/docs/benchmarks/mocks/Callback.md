---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 663.6 ns | 13.20 ns | 20.16 ns | 2.98 KB |
| Imposter | 478.4 ns | 8.21 ns | 7.68 ns | 2.66 KB |
| Mockolate | 534.8 ns | 7.57 ns | 6.71 ns | 1.8 KB |
| Moq | 135,092.5 ns | 1,145.41 ns | 1,015.38 ns | 13.29 KB |
| NSubstitute | 4,203.3 ns | 60.61 ns | 56.70 ns | 7.93 KB |
| FakeItEasy | 4,745.8 ns | 43.73 ns | 36.51 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162111
  bar [663.6, 478.4, 534.8, 135092.5, 4203.3, 4745.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 708.7 ns | 13.36 ns | 12.50 ns | 3.06 KB |
| Imposter | 540.9 ns | 5.44 ns | 5.09 ns | 2.82 KB |
| Mockolate | 689.4 ns | 8.90 ns | 8.32 ns | 2.13 KB |
| Moq | 142,126.8 ns | 698.95 ns | 583.65 ns | 13.85 KB |
| NSubstitute | 4,871.2 ns | 68.06 ns | 63.67 ns | 8.53 KB |
| FakeItEasy | 5,747.5 ns | 67.76 ns | 63.38 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 170553
  bar [708.7, 540.9, 689.4, 142126.8, 4871.2, 5747.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-30T03:25:10.403Z*
