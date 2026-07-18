---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 655.2 ns | 6.41 ns | 5.99 ns | 3.11 KB |
| Imposter | 471.4 ns | 5.40 ns | 5.05 ns | 2.66 KB |
| Mockolate | 347.4 ns | 2.36 ns | 2.21 ns | 1.8 KB |
| Moq | 133,770.5 ns | 1,422.05 ns | 1,260.61 ns | 13.14 KB |
| NSubstitute | 4,456.9 ns | 84.09 ns | 78.66 ns | 7.85 KB |
| FakeItEasy | 4,787.2 ns | 65.19 ns | 60.97 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 160525
  bar [655.2, 471.4, 347.4, 133770.5, 4456.9, 4787.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 771.7 ns | 3.27 ns | 2.90 ns | 3.2 KB |
| Imposter | 573.9 ns | 4.82 ns | 4.28 ns | 2.82 KB |
| Mockolate | 387.3 ns | 2.37 ns | 2.10 ns | 1.84 KB |
| Moq | 144,225.1 ns | 1,534.94 ns | 1,281.75 ns | 13.73 KB |
| NSubstitute | 4,914.5 ns | 51.14 ns | 42.70 ns | 8.41 KB |
| FakeItEasy | 5,963.3 ns | 43.31 ns | 38.40 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 173071
  bar [771.7, 573.9, 387.3, 144225.1, 4914.5, 5963.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-18T03:20:37.479Z*
