---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 682.4 ns | 6.81 ns | 6.37 ns | 3.13 KB |
| Imposter | 468.4 ns | 4.01 ns | 3.75 ns | 2.66 KB |
| Mockolate | 519.0 ns | 4.64 ns | 4.11 ns | 1.8 KB |
| Moq | 138,879.3 ns | 723.62 ns | 604.25 ns | 13.43 KB |
| NSubstitute | 4,185.1 ns | 44.87 ns | 37.47 ns | 7.93 KB |
| FakeItEasy | 4,675.3 ns | 61.64 ns | 57.66 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 166656
  bar [682.4, 468.4, 519, 138879.3, 4185.1, 4675.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 801.7 ns | 13.04 ns | 12.19 ns | 3.22 KB |
| Imposter | 532.3 ns | 2.54 ns | 2.37 ns | 2.82 KB |
| Mockolate | 697.4 ns | 2.30 ns | 2.15 ns | 2.13 KB |
| Moq | 141,818.1 ns | 1,536.63 ns | 1,437.37 ns | 13.73 KB |
| NSubstitute | 4,525.2 ns | 56.51 ns | 52.86 ns | 8.53 KB |
| FakeItEasy | 5,370.5 ns | 38.03 ns | 31.76 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 170182
  bar [801.7, 532.3, 697.4, 141818.1, 4525.2, 5370.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-11T03:20:45.459Z*
