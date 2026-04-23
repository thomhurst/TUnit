---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 685.5 ns | 10.52 ns | 9.84 ns | 3.13 KB |
| Imposter | 490.7 ns | 9.06 ns | 8.47 ns | 2.66 KB |
| Mockolate | 545.3 ns | 6.01 ns | 5.62 ns | 1.8 KB |
| Moq | 138,449.1 ns | 1,572.41 ns | 1,313.03 ns | 13.29 KB |
| NSubstitute | 4,359.4 ns | 65.98 ns | 58.49 ns | 7.93 KB |
| FakeItEasy | 4,788.6 ns | 93.23 ns | 87.21 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 166139
  bar [685.5, 490.7, 545.3, 138449.1, 4359.4, 4788.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 893.6 ns | 12.36 ns | 11.57 ns | 3.22 KB |
| Imposter | 560.4 ns | 8.55 ns | 8.00 ns | 2.82 KB |
| Mockolate | 696.2 ns | 5.53 ns | 5.17 ns | 2.13 KB |
| Moq | 141,647.1 ns | 738.60 ns | 654.75 ns | 13.73 KB |
| NSubstitute | 4,707.1 ns | 43.07 ns | 40.28 ns | 8.53 KB |
| FakeItEasy | 5,628.5 ns | 52.60 ns | 43.92 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 169977
  bar [893.6, 560.4, 696.2, 141647.1, 4707.1, 5628.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-23T03:25:34.373Z*
