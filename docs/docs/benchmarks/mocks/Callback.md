---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 668.2 ns | 2.21 ns | 1.96 ns | 3.08 KB |
| Imposter | 487.8 ns | 2.57 ns | 2.28 ns | 2.66 KB |
| Mockolate | 346.0 ns | 2.11 ns | 1.87 ns | 1.91 KB |
| Moq | 135,973.7 ns | 978.00 ns | 866.97 ns | 13.15 KB |
| NSubstitute | 4,044.3 ns | 42.81 ns | 35.75 ns | 7.93 KB |
| FakeItEasy | 4,499.5 ns | 39.30 ns | 36.76 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163169
  bar [668.2, 487.8, 346, 135973.7, 4044.3, 4499.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.9 ns | 6.16 ns | 5.46 ns | 3.16 KB |
| Imposter | 544.2 ns | 5.55 ns | 4.92 ns | 2.82 KB |
| Mockolate | 386.9 ns | 1.58 ns | 1.23 ns | 1.95 KB |
| Moq | 142,414.3 ns | 1,191.32 ns | 1,114.36 ns | 13.73 KB |
| NSubstitute | 4,686.3 ns | 19.08 ns | 17.85 ns | 8.53 KB |
| FakeItEasy | 5,427.7 ns | 25.37 ns | 21.18 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 170898
  bar [755.9, 544.2, 386.9, 142414.3, 4686.3, 5427.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-26T03:27:58.119Z*
