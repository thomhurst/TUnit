---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 856.8 ns | 6.03 ns | 5.64 ns | 3.13 KB |
| Imposter | 594.4 ns | 7.95 ns | 7.44 ns | 2.66 KB |
| Mockolate | 636.1 ns | 2.94 ns | 2.75 ns | 1.8 KB |
| Moq | 141,548.2 ns | 1,308.76 ns | 1,160.18 ns | 13.4 KB |
| NSubstitute | 4,178.4 ns | 14.88 ns | 13.19 ns | 7.93 KB |
| FakeItEasy | 5,212.8 ns | 15.61 ns | 13.84 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 169858
  bar [856.8, 594.4, 636.1, 141548.2, 4178.4, 5212.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 985.1 ns | 6.02 ns | 5.34 ns | 3.22 KB |
| Imposter | 654.4 ns | 4.99 ns | 4.67 ns | 2.82 KB |
| Mockolate | 769.4 ns | 6.99 ns | 6.20 ns | 2.13 KB |
| Moq | 144,856.5 ns | 1,080.28 ns | 957.64 ns | 13.75 KB |
| NSubstitute | 4,999.9 ns | 38.60 ns | 34.21 ns | 8.53 KB |
| FakeItEasy | 6,497.6 ns | 27.47 ns | 25.70 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173828
  bar [985.1, 654.4, 769.4, 144856.5, 4999.9, 6497.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-21T03:22:48.421Z*
