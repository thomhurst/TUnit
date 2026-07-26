---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 682.1 ns | 12.15 ns | 11.37 ns | 3.11 KB |
| Imposter | 470.0 ns | 5.79 ns | 5.42 ns | 2.66 KB |
| Mockolate | 348.5 ns | 5.56 ns | 5.20 ns | 1.8 KB |
| Moq | 188,377.4 ns | 1,780.20 ns | 1,578.10 ns | 13.14 KB |
| NSubstitute | 4,835.1 ns | 76.79 ns | 71.83 ns | 7.85 KB |
| FakeItEasy | 5,437.4 ns | 39.77 ns | 37.20 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 226053
  bar [682.1, 470, 348.5, 188377.4, 4835.1, 5437.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 814.1 ns | 14.91 ns | 13.95 ns | 3.2 KB |
| Imposter | 522.4 ns | 4.34 ns | 4.06 ns | 2.82 KB |
| Mockolate | 407.9 ns | 6.60 ns | 6.18 ns | 1.84 KB |
| Moq | 193,265.7 ns | 1,941.22 ns | 1,815.82 ns | 13.73 KB |
| NSubstitute | 5,387.6 ns | 80.79 ns | 75.57 ns | 8.41 KB |
| FakeItEasy | 6,398.6 ns | 124.08 ns | 103.61 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 231919
  bar [814.1, 522.4, 407.9, 193265.7, 5387.6, 6398.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-26T03:33:46.478Z*
