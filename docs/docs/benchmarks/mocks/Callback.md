---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 660.5 ns | 5.79 ns | 5.41 ns | 3.11 KB |
| Imposter | 459.7 ns | 3.23 ns | 3.02 ns | 2.66 KB |
| Mockolate | 351.0 ns | 2.90 ns | 2.71 ns | 1.8 KB |
| Moq | 135,807.3 ns | 1,092.65 ns | 912.41 ns | 13.29 KB |
| NSubstitute | 4,478.7 ns | 46.67 ns | 43.65 ns | 7.85 KB |
| FakeItEasy | 4,770.3 ns | 29.28 ns | 25.95 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162969
  bar [660.5, 459.7, 351, 135807.3, 4478.7, 4770.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 786.5 ns | 4.04 ns | 3.58 ns | 3.2 KB |
| Imposter | 538.6 ns | 1.39 ns | 1.09 ns | 2.82 KB |
| Mockolate | 410.9 ns | 6.75 ns | 5.98 ns | 1.84 KB |
| Moq | 144,420.7 ns | 1,375.11 ns | 1,286.28 ns | 13.73 KB |
| NSubstitute | 5,039.6 ns | 48.57 ns | 45.43 ns | 8.41 KB |
| FakeItEasy | 5,832.9 ns | 70.23 ns | 65.70 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173305
  bar [786.5, 538.6, 410.9, 144420.7, 5039.6, 5832.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-15T02:39:16.112Z*
