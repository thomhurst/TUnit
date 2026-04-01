---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 768.9 ns | 6.84 ns | 6.40 ns | 3.16 KB |
| Imposter | 514.7 ns | 4.61 ns | 4.31 ns | 2.66 KB |
| Mockolate | 510.0 ns | 8.03 ns | 6.70 ns | 1.78 KB |
| Moq | 183,100.0 ns | 656.42 ns | 614.01 ns | 13.25 KB |
| NSubstitute | 4,444.2 ns | 30.41 ns | 25.39 ns | 7.93 KB |
| FakeItEasy | 5,142.2 ns | 18.16 ns | 15.17 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 219720
  bar [768.9, 514.7, 510, 183100, 4444.2, 5142.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 845.2 ns | 9.78 ns | 8.16 ns | 3.33 KB |
| Imposter | 521.4 ns | 6.29 ns | 5.89 ns | 2.82 KB |
| Mockolate | 634.8 ns | 4.07 ns | 3.61 ns | 2.11 KB |
| Moq | 188,308.8 ns | 570.84 ns | 476.68 ns | 13.73 KB |
| NSubstitute | 4,931.6 ns | 35.48 ns | 31.45 ns | 8.53 KB |
| FakeItEasy | 6,191.6 ns | 100.59 ns | 94.09 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 225971
  bar [845.2, 521.4, 634.8, 188308.8, 4931.6, 6191.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-01T03:22:34.139Z*
