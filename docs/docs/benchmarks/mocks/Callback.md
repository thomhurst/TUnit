---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 760.4 ns | 14.87 ns | 15.27 ns | 3.11 KB |
| Imposter | 482.9 ns | 8.45 ns | 14.80 ns | 2.66 KB |
| Mockolate | 366.5 ns | 7.29 ns | 10.90 ns | 1.8 KB |
| Moq | 186,333.5 ns | 1,744.04 ns | 1,631.37 ns | 13.14 KB |
| NSubstitute | 4,731.2 ns | 58.87 ns | 52.19 ns | 7.93 KB |
| FakeItEasy | 5,382.5 ns | 56.56 ns | 52.91 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223601
  bar [760.4, 482.9, 366.5, 186333.5, 4731.2, 5382.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 832.2 ns | 15.83 ns | 14.81 ns | 3.2 KB |
| Imposter | 594.9 ns | 11.24 ns | 9.97 ns | 2.82 KB |
| Mockolate | 481.1 ns | 9.47 ns | 9.30 ns | 1.84 KB |
| Moq | 194,628.0 ns | 722.71 ns | 676.02 ns | 13.73 KB |
| NSubstitute | 5,513.9 ns | 30.26 ns | 28.31 ns | 8.53 KB |
| FakeItEasy | 6,800.9 ns | 50.86 ns | 47.58 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 233554
  bar [832.2, 594.9, 481.1, 194628, 5513.9, 6800.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-07T03:24:42.900Z*
