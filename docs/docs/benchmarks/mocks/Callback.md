---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 746.3 ns | 14.71 ns | 23.75 ns | 3.11 KB |
| Imposter | 501.4 ns | 9.58 ns | 9.41 ns | 2.66 KB |
| Mockolate | 409.8 ns | 7.53 ns | 7.04 ns | 1.8 KB |
| Moq | 138,077.9 ns | 713.08 ns | 632.12 ns | 13.24 KB |
| NSubstitute | 4,183.4 ns | 60.09 ns | 56.21 ns | 7.93 KB |
| FakeItEasy | 5,009.5 ns | 59.77 ns | 52.99 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 165694
  bar [746.3, 501.4, 409.8, 138077.9, 4183.4, 5009.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 898.5 ns | 13.54 ns | 12.00 ns | 3.2 KB |
| Imposter | 599.3 ns | 11.93 ns | 13.73 ns | 2.82 KB |
| Mockolate | 455.7 ns | 4.92 ns | 4.36 ns | 1.84 KB |
| Moq | 149,322.0 ns | 1,268.07 ns | 1,186.15 ns | 13.75 KB |
| NSubstitute | 4,764.6 ns | 57.95 ns | 54.20 ns | 8.53 KB |
| FakeItEasy | 6,468.4 ns | 67.88 ns | 63.50 ns | 9.38 KB |

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
  y-axis "Time (ns)" 0 --> 179187
  bar [898.5, 599.3, 455.7, 149322, 4764.6, 6468.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-26T03:28:53.126Z*
