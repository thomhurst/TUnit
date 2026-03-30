---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.2 ns | 11.54 ns | 10.79 ns | 3.72 KB |
| Imposter | 452.3 ns | 2.77 ns | 2.59 ns | 2.66 KB |
| Mockolate | 519.3 ns | 2.93 ns | 2.60 ns | 1.84 KB |
| Moq | 178,505.1 ns | 942.35 ns | 881.48 ns | 13.14 KB |
| NSubstitute | 4,419.0 ns | 50.22 ns | 44.52 ns | 7.93 KB |
| FakeItEasy | 5,227.9 ns | 53.06 ns | 47.03 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 214207
  bar [740.2, 452.3, 519.3, 178505.1, 4419, 5227.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 882.0 ns | 9.09 ns | 8.50 ns | 3.8 KB |
| Imposter | 528.8 ns | 3.37 ns | 2.99 ns | 2.82 KB |
| Mockolate | 678.0 ns | 4.77 ns | 3.98 ns | 2.22 KB |
| Moq | 189,676.3 ns | 904.24 ns | 801.59 ns | 13.85 KB |
| NSubstitute | 5,288.4 ns | 66.31 ns | 62.02 ns | 8.53 KB |
| FakeItEasy | 6,535.3 ns | 101.49 ns | 89.97 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 227612
  bar [882, 528.8, 678, 189676.3, 5288.4, 6535.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T01:06:26.815Z*
