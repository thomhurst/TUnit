---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 611.7 ns | 3.24 ns | 2.87 ns | 2.98 KB |
| Imposter | 448.0 ns | 0.87 ns | 0.68 ns | 2.66 KB |
| Mockolate | 354.2 ns | 7.05 ns | 9.88 ns | 1.89 KB |
| Moq | 180,351.2 ns | 550.15 ns | 514.61 ns | 13.14 KB |
| NSubstitute | 4,464.3 ns | 20.77 ns | 18.41 ns | 7.93 KB |
| FakeItEasy | 5,063.1 ns | 21.07 ns | 19.71 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 216422
  bar [611.7, 448, 354.2, 180351.2, 4464.3, 5063.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 706.7 ns | 1.87 ns | 1.75 ns | 3.06 KB |
| Imposter | 512.7 ns | 1.75 ns | 1.64 ns | 2.82 KB |
| Mockolate | 392.2 ns | 1.45 ns | 1.13 ns | 1.94 KB |
| Moq | 189,124.1 ns | 1,331.75 ns | 1,180.56 ns | 13.73 KB |
| NSubstitute | 5,249.4 ns | 81.66 ns | 72.39 ns | 8.53 KB |
| FakeItEasy | 6,283.3 ns | 81.24 ns | 75.99 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 226949
  bar [706.7, 512.7, 392.2, 189124.1, 5249.4, 6283.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-06T03:25:44.139Z*
