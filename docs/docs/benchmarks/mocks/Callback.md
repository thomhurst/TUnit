---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 682.9 ns | 9.65 ns | 9.03 ns | 3.16 KB |
| Imposter | 451.9 ns | 4.14 ns | 3.87 ns | 2.66 KB |
| Mockolate | 508.8 ns | 9.65 ns | 9.03 ns | 1.8 KB |
| Moq | 181,841.2 ns | 1,417.67 ns | 1,326.09 ns | 13.14 KB |
| NSubstitute | 4,471.9 ns | 67.49 ns | 63.13 ns | 7.93 KB |
| FakeItEasy | 5,343.1 ns | 44.61 ns | 34.83 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218210
  bar [682.9, 451.9, 508.8, 181841.2, 4471.9, 5343.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 795.5 ns | 14.07 ns | 13.17 ns | 3.33 KB |
| Imposter | 543.2 ns | 7.43 ns | 6.95 ns | 2.82 KB |
| Mockolate | 652.2 ns | 9.75 ns | 9.12 ns | 2.13 KB |
| Moq | 186,597.9 ns | 928.12 ns | 822.76 ns | 13.73 KB |
| NSubstitute | 4,959.0 ns | 26.93 ns | 25.19 ns | 8.53 KB |
| FakeItEasy | 6,610.9 ns | 130.93 ns | 134.45 ns | 9.34 KB |

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
  y-axis "Time (ns)" 0 --> 223918
  bar [795.5, 543.2, 652.2, 186597.9, 4959, 6610.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-04T03:18:30.135Z*
