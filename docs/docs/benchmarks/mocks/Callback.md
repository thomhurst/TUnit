---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 620.3 ns | 4.76 ns | 4.45 ns | 2.98 KB |
| Imposter | 458.8 ns | 2.83 ns | 2.36 ns | 2.66 KB |
| Mockolate | 498.5 ns | 2.07 ns | 1.72 ns | 1.8 KB |
| Moq | 182,179.2 ns | 1,557.09 ns | 1,456.50 ns | 13.14 KB |
| NSubstitute | 4,389.4 ns | 16.88 ns | 14.97 ns | 7.93 KB |
| FakeItEasy | 4,984.7 ns | 23.06 ns | 20.44 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218616
  bar [620.3, 458.8, 498.5, 182179.2, 4389.4, 4984.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 691.1 ns | 1.02 ns | 0.86 ns | 3.06 KB |
| Imposter | 519.9 ns | 1.97 ns | 1.84 ns | 2.82 KB |
| Mockolate | 624.8 ns | 0.97 ns | 0.86 ns | 2.13 KB |
| Moq | 190,238.9 ns | 1,825.66 ns | 1,707.72 ns | 13.73 KB |
| NSubstitute | 5,092.5 ns | 15.67 ns | 13.89 ns | 8.53 KB |
| FakeItEasy | 6,233.8 ns | 72.03 ns | 63.85 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 228287
  bar [691.1, 519.9, 624.8, 190238.9, 5092.5, 6233.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-28T03:25:54.642Z*
