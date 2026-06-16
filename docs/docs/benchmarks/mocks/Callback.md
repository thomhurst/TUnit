---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 694.7 ns | 9.40 ns | 8.79 ns | 3.11 KB |
| Imposter | 475.1 ns | 4.83 ns | 4.29 ns | 2.66 KB |
| Mockolate | 361.1 ns | 5.03 ns | 4.71 ns | 1.91 KB |
| Moq | 136,205.6 ns | 1,348.19 ns | 1,195.14 ns | 13.14 KB |
| NSubstitute | 4,178.8 ns | 55.15 ns | 48.89 ns | 7.93 KB |
| FakeItEasy | 4,566.5 ns | 72.49 ns | 64.26 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163447
  bar [694.7, 475.1, 361.1, 136205.6, 4178.8, 4566.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 770.4 ns | 6.81 ns | 5.69 ns | 3.2 KB |
| Imposter | 540.8 ns | 5.69 ns | 5.32 ns | 2.82 KB |
| Mockolate | 402.6 ns | 3.78 ns | 3.35 ns | 1.95 KB |
| Moq | 145,009.6 ns | 1,001.89 ns | 888.15 ns | 13.73 KB |
| NSubstitute | 4,693.5 ns | 44.49 ns | 39.44 ns | 8.53 KB |
| FakeItEasy | 5,793.0 ns | 102.11 ns | 109.26 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 174012
  bar [770.4, 540.8, 402.6, 145009.6, 4693.5, 5793]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-16T03:29:20.737Z*
