---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 279.3 ns | 76.48 ns | 4.19 ns | 128 B |
| Imposter | 298.0 ns | 49.11 ns | 2.69 ns | 168 B |
| Mockolate | 122.6 ns | 63.47 ns | 3.48 ns | 84 B |
| Moq | 876.9 ns | 94.52 ns | 5.18 ns | 376 B |
| NSubstitute | 754.4 ns | 156.93 ns | 8.60 ns | 304 B |
| FakeItEasy | 1,895.6 ns | 817.92 ns | 44.83 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2275
  bar [279.3, 298, 122.6, 876.9, 754.4, 1895.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.5 ns | 63.53 ns | 3.48 ns | 96 B |
| Imposter | 297.6 ns | 77.79 ns | 4.26 ns | 168 B |
| Mockolate | 105.2 ns | 67.44 ns | 3.70 ns | 60 B |
| Moq | 592.3 ns | 33.06 ns | 1.81 ns | 296 B |
| NSubstitute | 658.2 ns | 550.28 ns | 30.16 ns | 272 B |
| FakeItEasy | 1,631.3 ns | 544.20 ns | 29.83 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1958
  bar [166.5, 297.6, 105.2, 592.3, 658.2, 1631.3]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,070.9 ns | 5,437.02 ns | 298.02 ns | 12736 B |
| Imposter | 30,681.9 ns | 1,307.32 ns | 71.66 ns | 16800 B |
| Mockolate | 12,870.2 ns | 3,679.64 ns | 201.69 ns | 8400 B |
| Moq | 88,694.5 ns | 38,508.58 ns | 2,110.79 ns | 37600 B |
| NSubstitute | 75,244.9 ns | 33,836.94 ns | 1,854.72 ns | 30848 B |
| FakeItEasy | 198,561.0 ns | 56,800.96 ns | 3,113.45 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 238274
  bar [28070.9, 30681.9, 12870.2, 88694.5, 75244.9, 198561]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-10T03:28:13.506Z*
