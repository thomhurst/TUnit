---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 609.5 ns | 10.82 ns | 10.12 ns | 2.98 KB |
| Imposter | 491.2 ns | 9.44 ns | 10.49 ns | 2.66 KB |
| Mockolate | 534.4 ns | 6.52 ns | 5.78 ns | 1.8 KB |
| Moq | 134,836.4 ns | 1,454.52 ns | 1,360.56 ns | 13.14 KB |
| NSubstitute | 4,094.8 ns | 47.02 ns | 43.98 ns | 7.93 KB |
| FakeItEasy | 4,547.9 ns | 62.17 ns | 58.15 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 161804
  bar [609.5, 491.2, 534.4, 134836.4, 4094.8, 4547.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 703.7 ns | 11.59 ns | 10.84 ns | 3.06 KB |
| Imposter | 538.8 ns | 3.06 ns | 2.39 ns | 2.82 KB |
| Mockolate | 669.6 ns | 4.83 ns | 4.52 ns | 2.13 KB |
| Moq | 141,701.8 ns | 1,094.95 ns | 970.65 ns | 13.73 KB |
| NSubstitute | 4,627.0 ns | 26.23 ns | 24.53 ns | 8.53 KB |
| FakeItEasy | 5,934.1 ns | 117.58 ns | 172.34 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 170043
  bar [703.7, 538.8, 669.6, 141701.8, 4627, 5934.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-26T03:29:14.435Z*
