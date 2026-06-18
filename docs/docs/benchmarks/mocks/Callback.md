---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 697.7 ns | 9.43 ns | 8.82 ns | 3.11 KB |
| Imposter | 469.3 ns | 4.98 ns | 4.66 ns | 2.66 KB |
| Mockolate | 376.3 ns | 3.17 ns | 2.81 ns | 1.91 KB |
| Moq | 183,861.5 ns | 781.51 ns | 731.03 ns | 13.14 KB |
| NSubstitute | 4,478.3 ns | 32.83 ns | 27.41 ns | 7.93 KB |
| FakeItEasy | 5,088.7 ns | 31.41 ns | 29.38 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 220634
  bar [697.7, 469.3, 376.3, 183861.5, 4478.3, 5088.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 847.6 ns | 9.73 ns | 9.10 ns | 3.2 KB |
| Imposter | 543.8 ns | 4.98 ns | 3.89 ns | 2.82 KB |
| Mockolate | 411.8 ns | 8.07 ns | 7.15 ns | 1.95 KB |
| Moq | 193,329.5 ns | 1,121.02 ns | 993.75 ns | 13.73 KB |
| NSubstitute | 5,305.6 ns | 56.04 ns | 49.68 ns | 8.53 KB |
| FakeItEasy | 6,500.5 ns | 113.72 ns | 121.68 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 231996
  bar [847.6, 543.8, 411.8, 193329.5, 5305.6, 6500.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-18T03:29:53.480Z*
