---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 771.8 ns | 15.32 ns | 14.33 ns | 3.1 KB |
| Imposter | 497.5 ns | 5.40 ns | 4.79 ns | 2.66 KB |
| Mockolate | 398.5 ns | 7.69 ns | 8.23 ns | 1.91 KB |
| Moq | 141,563.2 ns | 1,233.24 ns | 1,153.57 ns | 13.29 KB |
| NSubstitute | 4,143.9 ns | 21.90 ns | 18.29 ns | 7.93 KB |
| FakeItEasy | 5,182.6 ns | 58.92 ns | 52.23 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 169876
  bar [771.8, 497.5, 398.5, 141563.2, 4143.9, 5182.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 928.7 ns | 11.06 ns | 10.35 ns | 3.2 KB |
| Imposter | 551.3 ns | 6.75 ns | 5.99 ns | 2.82 KB |
| Mockolate | 453.6 ns | 7.30 ns | 6.09 ns | 1.95 KB |
| Moq | 147,540.9 ns | 1,291.96 ns | 1,145.29 ns | 13.75 KB |
| NSubstitute | 4,741.2 ns | 45.91 ns | 42.95 ns | 8.53 KB |
| FakeItEasy | 6,236.5 ns | 41.70 ns | 39.01 ns | 9.41 KB |

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
  y-axis "Time (ns)" 0 --> 177050
  bar [928.7, 551.3, 453.6, 147540.9, 4741.2, 6236.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-06T03:26:59.455Z*
