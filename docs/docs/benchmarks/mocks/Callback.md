---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 840.6 ns | 16.72 ns | 19.91 ns | 3.13 KB |
| Imposter | 560.5 ns | 10.89 ns | 11.65 ns | 2.66 KB |
| Mockolate | 614.4 ns | 6.70 ns | 5.94 ns | 1.8 KB |
| Moq | 140,130.3 ns | 897.78 ns | 839.79 ns | 13.29 KB |
| NSubstitute | 4,307.0 ns | 25.01 ns | 23.40 ns | 7.93 KB |
| FakeItEasy | 5,535.4 ns | 32.79 ns | 29.07 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 168157
  bar [840.6, 560.5, 614.4, 140130.3, 4307, 5535.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 949.3 ns | 7.73 ns | 7.23 ns | 3.22 KB |
| Imposter | 625.3 ns | 12.27 ns | 13.12 ns | 2.82 KB |
| Mockolate | 762.0 ns | 7.65 ns | 7.16 ns | 2.13 KB |
| Moq | 145,388.9 ns | 1,028.96 ns | 912.14 ns | 13.75 KB |
| NSubstitute | 4,890.5 ns | 27.56 ns | 23.01 ns | 8.53 KB |
| FakeItEasy | 6,437.6 ns | 51.02 ns | 47.72 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 174467
  bar [949.3, 625.3, 762, 145388.9, 4890.5, 6437.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-08T03:21:46.624Z*
