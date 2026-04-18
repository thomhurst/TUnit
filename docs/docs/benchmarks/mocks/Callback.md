---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 788.7 ns | 15.71 ns | 23.98 ns | 3.13 KB |
| Imposter | 498.9 ns | 5.24 ns | 4.38 ns | 2.66 KB |
| Mockolate | 605.7 ns | 7.53 ns | 7.04 ns | 1.8 KB |
| Moq | 142,651.5 ns | 1,316.03 ns | 1,166.63 ns | 13.15 KB |
| NSubstitute | 4,178.9 ns | 29.61 ns | 27.70 ns | 7.93 KB |
| FakeItEasy | 5,247.9 ns | 29.16 ns | 25.85 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 171182
  bar [788.7, 498.9, 605.7, 142651.5, 4178.9, 5247.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 908.8 ns | 12.77 ns | 11.95 ns | 3.22 KB |
| Imposter | 557.2 ns | 6.41 ns | 6.00 ns | 2.82 KB |
| Mockolate | 716.3 ns | 8.23 ns | 7.29 ns | 2.13 KB |
| Moq | 146,076.4 ns | 700.76 ns | 585.17 ns | 13.75 KB |
| NSubstitute | 5,046.6 ns | 53.96 ns | 47.83 ns | 8.53 KB |
| FakeItEasy | 6,906.8 ns | 72.11 ns | 67.45 ns | 9.41 KB |

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
  y-axis "Time (ns)" 0 --> 175292
  bar [908.8, 557.2, 716.3, 146076.4, 5046.6, 6906.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-18T03:21:40.293Z*
