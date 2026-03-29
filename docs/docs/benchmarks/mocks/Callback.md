---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 2,195.4 ns | 26.36 ns | 24.66 ns | 3.94 KB |
| Imposter | 445.5 ns | 8.32 ns | 9.58 ns | 2.66 KB |
| Mockolate | 520.7 ns | 10.23 ns | 11.78 ns | 1.84 KB |
| Moq | 184,574.2 ns | 1,199.27 ns | 1,063.13 ns | 13.14 KB |
| NSubstitute | 4,537.7 ns | 63.95 ns | 59.82 ns | 7.93 KB |
| FakeItEasy | 5,401.6 ns | 76.22 ns | 71.29 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221490
  bar [2195.4, 445.5, 520.7, 184574.2, 4537.7, 5401.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 2,310.8 ns | 43.61 ns | 40.79 ns | 4.04 KB |
| Imposter | 626.2 ns | 12.52 ns | 16.28 ns | 2.82 KB |
| Mockolate | 689.5 ns | 11.15 ns | 10.96 ns | 2.22 KB |
| Moq | 193,672.7 ns | 1,246.68 ns | 1,105.15 ns | 13.73 KB |
| NSubstitute | 5,119.9 ns | 100.13 ns | 93.66 ns | 8.53 KB |
| FakeItEasy | 6,185.7 ns | 119.41 ns | 132.72 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 232408
  bar [2310.8, 626.2, 689.5, 193672.7, 5119.9, 6185.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-29T22:20:59.126Z*
