---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 647.8 ns | 1.12 ns | 0.99 ns | 3.11 KB |
| Imposter | 453.4 ns | 0.54 ns | 0.48 ns | 2.66 KB |
| Mockolate | 344.3 ns | 1.87 ns | 1.75 ns | 1.8 KB |
| Moq | 135,068.8 ns | 680.06 ns | 602.86 ns | 13.29 KB |
| NSubstitute | 4,503.9 ns | 29.21 ns | 24.39 ns | 7.85 KB |
| FakeItEasy | 4,543.5 ns | 40.66 ns | 36.04 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162083
  bar [647.8, 453.4, 344.3, 135068.8, 4503.9, 4543.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 757.4 ns | 1.90 ns | 1.48 ns | 3.2 KB |
| Imposter | 532.1 ns | 1.25 ns | 1.11 ns | 2.82 KB |
| Mockolate | 387.8 ns | 1.90 ns | 1.78 ns | 1.84 KB |
| Moq | 141,701.3 ns | 1,073.92 ns | 896.77 ns | 13.73 KB |
| NSubstitute | 4,964.8 ns | 18.89 ns | 16.74 ns | 8.41 KB |
| FakeItEasy | 5,455.2 ns | 26.03 ns | 23.07 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 170042
  bar [757.4, 532.1, 387.8, 141701.3, 4964.8, 5455.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-27T04:05:27.840Z*
