---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 765.0 ns | 15.24 ns | 31.80 ns | 3.14 KB |
| Imposter | 509.2 ns | 10.02 ns | 14.37 ns | 2.66 KB |
| Mockolate | 536.5 ns | 8.07 ns | 7.55 ns | 1.8 KB |
| Moq | 183,837.1 ns | 1,611.77 ns | 1,507.65 ns | 13.14 KB |
| NSubstitute | 4,625.2 ns | 16.29 ns | 13.60 ns | 7.93 KB |
| FakeItEasy | 5,742.1 ns | 48.51 ns | 43.00 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 220605
  bar [765, 509.2, 536.5, 183837.1, 4625.2, 5742.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 906.3 ns | 5.45 ns | 5.10 ns | 3.35 KB |
| Imposter | 609.7 ns | 7.90 ns | 7.00 ns | 2.82 KB |
| Mockolate | 693.1 ns | 5.12 ns | 4.54 ns | 2.13 KB |
| Moq | 200,810.5 ns | 4,012.17 ns | 8,285.83 ns | 13.74 KB |
| NSubstitute | 5,251.6 ns | 48.17 ns | 40.22 ns | 8.53 KB |
| FakeItEasy | 6,672.2 ns | 128.08 ns | 119.81 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 240973
  bar [906.3, 609.7, 693.1, 200810.5, 5251.6, 6672.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T11:44:06.333Z*
