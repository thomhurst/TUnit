---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 603.1 ns | 5.33 ns | 4.99 ns | 2.98 KB |
| Imposter | 515.1 ns | 5.65 ns | 5.28 ns | 2.66 KB |
| Mockolate | 544.8 ns | 9.41 ns | 8.80 ns | 1.8 KB |
| Moq | 133,087.7 ns | 828.33 ns | 774.82 ns | 13.24 KB |
| NSubstitute | 4,112.0 ns | 40.45 ns | 33.78 ns | 7.93 KB |
| FakeItEasy | 4,502.3 ns | 38.23 ns | 29.85 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 159706
  bar [603.1, 515.1, 544.8, 133087.7, 4112, 4502.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 707.1 ns | 2.87 ns | 2.24 ns | 3.06 KB |
| Imposter | 535.3 ns | 4.12 ns | 3.66 ns | 2.82 KB |
| Mockolate | 668.7 ns | 5.56 ns | 5.20 ns | 2.13 KB |
| Moq | 138,927.4 ns | 1,108.75 ns | 1,037.13 ns | 13.73 KB |
| NSubstitute | 4,535.2 ns | 14.03 ns | 13.12 ns | 8.53 KB |
| FakeItEasy | 5,459.7 ns | 28.92 ns | 25.64 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 166713
  bar [707.1, 535.3, 668.7, 138927.4, 4535.2, 5459.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-24T03:24:24.137Z*
