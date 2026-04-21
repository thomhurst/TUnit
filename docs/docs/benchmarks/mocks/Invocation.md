---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 260.4 ns | 70.28 ns | 3.85 ns | 120 B |
| Imposter | 296.2 ns | 65.85 ns | 3.61 ns | 168 B |
| Mockolate | 666.8 ns | 217.72 ns | 11.93 ns | 640 B |
| Moq | 827.7 ns | 192.73 ns | 10.56 ns | 376 B |
| NSubstitute | 754.9 ns | 107.61 ns | 5.90 ns | 304 B |
| FakeItEasy | 1,785.3 ns | 497.47 ns | 27.27 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2143
  bar [260.4, 296.2, 666.8, 827.7, 754.9, 1785.3]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 156.7 ns | 65.52 ns | 3.59 ns | 88 B |
| Imposter | 295.4 ns | 170.85 ns | 9.36 ns | 168 B |
| Mockolate | 527.5 ns | 175.12 ns | 9.60 ns | 520 B |
| Moq | 536.6 ns | 75.02 ns | 4.11 ns | 296 B |
| NSubstitute | 606.6 ns | 276.20 ns | 15.14 ns | 272 B |
| FakeItEasy | 1,549.4 ns | 205.51 ns | 11.26 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1860
  bar [156.7, 295.4, 527.5, 536.6, 606.6, 1549.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,187.0 ns | 11,191.09 ns | 613.42 ns | 11936 B |
| Imposter | 28,798.8 ns | 10,620.61 ns | 582.15 ns | 16800 B |
| Mockolate | 66,320.1 ns | 25,726.27 ns | 1,410.14 ns | 64000 B |
| Moq | 80,930.1 ns | 29,407.92 ns | 1,611.95 ns | 37600 B |
| NSubstitute | 80,234.5 ns | 12,229.96 ns | 670.37 ns | 30848 B |
| FakeItEasy | 176,371.4 ns | 82,382.91 ns | 4,515.69 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 211646
  bar [26187, 28798.8, 66320.1, 80930.1, 80234.5, 176371.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-21T03:22:48.421Z*
