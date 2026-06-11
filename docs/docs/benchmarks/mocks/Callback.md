---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 650.9 ns | 2.30 ns | 1.92 ns | 3.1 KB |
| Imposter | 464.6 ns | 1.05 ns | 0.93 ns | 2.66 KB |
| Mockolate | 352.1 ns | 2.17 ns | 2.03 ns | 1.91 KB |
| Moq | 134,857.9 ns | 905.86 ns | 847.34 ns | 13.14 KB |
| NSubstitute | 4,092.1 ns | 10.11 ns | 8.96 ns | 7.93 KB |
| FakeItEasy | 4,659.7 ns | 21.69 ns | 20.29 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 161830
  bar [650.9, 464.6, 352.1, 134857.9, 4092.1, 4659.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 754.0 ns | 1.22 ns | 1.08 ns | 3.2 KB |
| Imposter | 547.2 ns | 7.13 ns | 6.67 ns | 2.82 KB |
| Mockolate | 413.3 ns | 5.41 ns | 5.06 ns | 1.95 KB |
| Moq | 145,769.6 ns | 1,027.80 ns | 961.41 ns | 13.73 KB |
| NSubstitute | 4,606.2 ns | 16.36 ns | 13.66 ns | 8.53 KB |
| FakeItEasy | 5,537.7 ns | 22.57 ns | 21.11 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 174924
  bar [754, 547.2, 413.3, 145769.6, 4606.2, 5537.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-11T03:26:37.062Z*
