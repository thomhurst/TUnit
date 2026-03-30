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
| **TUnit.Mocks** | 809.8 ns | 16.24 ns | 26.68 ns | 3.89 KB |
| Imposter | 538.4 ns | 9.49 ns | 8.88 ns | 2.66 KB |
| Mockolate | 582.6 ns | 11.52 ns | 11.83 ns | 1.78 KB |
| Moq | 187,458.7 ns | 1,104.10 ns | 978.75 ns | 13.14 KB |
| NSubstitute | 4,664.1 ns | 61.25 ns | 57.29 ns | 7.93 KB |
| FakeItEasy | 5,376.9 ns | 104.63 ns | 97.87 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 224951
  bar [809.8, 538.4, 582.6, 187458.7, 4664.1, 5376.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 874.4 ns | 17.30 ns | 25.89 ns | 4.06 KB |
| Imposter | 566.8 ns | 11.25 ns | 15.78 ns | 2.82 KB |
| Mockolate | 698.7 ns | 13.14 ns | 12.91 ns | 2.11 KB |
| Moq | 194,745.5 ns | 1,594.82 ns | 1,413.76 ns | 13.73 KB |
| NSubstitute | 5,126.7 ns | 42.79 ns | 37.94 ns | 8.53 KB |
| FakeItEasy | 6,496.5 ns | 60.62 ns | 53.73 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 233695
  bar [874.4, 566.8, 698.7, 194745.5, 5126.7, 6496.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T21:56:59.028Z*
