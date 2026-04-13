---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 694.2 ns | 8.14 ns | 7.62 ns | 3.13 KB |
| Imposter | 485.9 ns | 5.01 ns | 4.69 ns | 2.66 KB |
| Mockolate | 523.1 ns | 7.18 ns | 6.71 ns | 1.8 KB |
| Moq | 136,768.2 ns | 1,525.34 ns | 1,352.18 ns | 13.14 KB |
| NSubstitute | 4,060.7 ns | 46.02 ns | 38.43 ns | 7.93 KB |
| FakeItEasy | 4,677.9 ns | 50.77 ns | 42.39 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 164122
  bar [694.2, 485.9, 523.1, 136768.2, 4060.7, 4677.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 827.5 ns | 15.46 ns | 15.88 ns | 3.22 KB |
| Imposter | 577.5 ns | 9.26 ns | 8.21 ns | 2.82 KB |
| Mockolate | 659.4 ns | 10.40 ns | 9.73 ns | 2.13 KB |
| Moq | 142,538.6 ns | 1,273.14 ns | 1,128.60 ns | 13.73 KB |
| NSubstitute | 4,639.0 ns | 41.31 ns | 38.64 ns | 8.53 KB |
| FakeItEasy | 5,757.7 ns | 22.89 ns | 17.87 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 171047
  bar [827.5, 577.5, 659.4, 142538.6, 4639, 5757.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-13T03:23:34.678Z*
