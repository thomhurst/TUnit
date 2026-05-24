---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 633.0 ns | 2.74 ns | 2.56 ns | 3.08 KB |
| Imposter | 452.0 ns | 1.11 ns | 0.98 ns | 2.66 KB |
| Mockolate | 334.6 ns | 0.63 ns | 0.53 ns | 1.91 KB |
| Moq | 182,032.9 ns | 888.87 ns | 742.25 ns | 13.14 KB |
| NSubstitute | 4,305.4 ns | 9.83 ns | 9.20 ns | 7.93 KB |
| FakeItEasy | 5,155.0 ns | 11.96 ns | 11.19 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218440
  bar [633, 452, 334.6, 182032.9, 4305.4, 5155]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 776.8 ns | 1.61 ns | 1.43 ns | 3.16 KB |
| Imposter | 505.8 ns | 1.29 ns | 1.08 ns | 2.82 KB |
| Mockolate | 393.9 ns | 1.09 ns | 0.97 ns | 1.95 KB |
| Moq | 192,734.1 ns | 1,236.70 ns | 1,096.30 ns | 13.73 KB |
| NSubstitute | 4,978.4 ns | 27.90 ns | 26.10 ns | 8.53 KB |
| FakeItEasy | 6,121.0 ns | 66.10 ns | 55.20 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 231281
  bar [776.8, 505.8, 393.9, 192734.1, 4978.4, 6121]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-24T03:32:03.972Z*
