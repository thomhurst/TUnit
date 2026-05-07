---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 605.0 ns | 11.63 ns | 11.42 ns | 2.98 KB |
| Imposter | 488.9 ns | 7.92 ns | 7.02 ns | 2.66 KB |
| Mockolate | 356.9 ns | 5.00 ns | 4.67 ns | 1.89 KB |
| Moq | 182,052.0 ns | 1,447.27 ns | 1,282.97 ns | 13.14 KB |
| NSubstitute | 4,457.5 ns | 44.77 ns | 39.69 ns | 7.93 KB |
| FakeItEasy | 5,020.6 ns | 63.90 ns | 59.77 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218463
  bar [605, 488.9, 356.9, 182052, 4457.5, 5020.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 708.9 ns | 4.91 ns | 4.35 ns | 3.06 KB |
| Imposter | 515.8 ns | 1.39 ns | 1.16 ns | 2.82 KB |
| Mockolate | 407.0 ns | 1.81 ns | 1.60 ns | 1.94 KB |
| Moq | 190,047.9 ns | 1,366.44 ns | 1,141.04 ns | 13.73 KB |
| NSubstitute | 5,119.8 ns | 71.54 ns | 63.42 ns | 8.53 KB |
| FakeItEasy | 6,508.2 ns | 82.74 ns | 73.34 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 228058
  bar [708.9, 515.8, 407, 190047.9, 5119.8, 6508.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-07T03:27:11.074Z*
