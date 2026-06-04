---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 637.4 ns | 3.21 ns | 2.85 ns | 3.1 KB |
| Imposter | 464.2 ns | 1.64 ns | 1.37 ns | 2.66 KB |
| Mockolate | 344.6 ns | 1.59 ns | 1.41 ns | 1.91 KB |
| Moq | 132,087.7 ns | 1,222.29 ns | 1,083.53 ns | 13.14 KB |
| NSubstitute | 4,056.6 ns | 21.20 ns | 18.79 ns | 7.93 KB |
| FakeItEasy | 4,442.5 ns | 43.18 ns | 38.27 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 158506
  bar [637.4, 464.2, 344.6, 132087.7, 4056.6, 4442.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 766.2 ns | 2.25 ns | 1.99 ns | 3.2 KB |
| Imposter | 546.9 ns | 6.10 ns | 5.41 ns | 2.82 KB |
| Mockolate | 392.5 ns | 3.32 ns | 3.11 ns | 1.95 KB |
| Moq | 144,708.3 ns | 2,501.13 ns | 2,676.18 ns | 13.73 KB |
| NSubstitute | 4,731.2 ns | 29.12 ns | 24.32 ns | 8.53 KB |
| FakeItEasy | 5,535.4 ns | 24.92 ns | 22.09 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173650
  bar [766.2, 546.9, 392.5, 144708.3, 4731.2, 5535.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-04T03:31:56.363Z*
