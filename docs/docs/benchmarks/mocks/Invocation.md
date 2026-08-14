---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 268.45 ns | 79.80 ns | 4.374 ns | 128 B |
| Imposter | 289.00 ns | 93.15 ns | 5.106 ns | 168 B |
| Mockolate | 101.83 ns | 11.66 ns | 0.639 ns | 84 B |
| Moq | 793.18 ns | 106.36 ns | 5.830 ns | 376 B |
| NSubstitute | 705.40 ns | 182.39 ns | 9.997 ns | 304 B |
| FakeItEasy | 1,719.24 ns | 1,055.03 ns | 57.830 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2064
  bar [268.45, 289, 101.83, 793.18, 705.4, 1719.24]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.63 ns | 73.61 ns | 4.035 ns | 96 B |
| Imposter | 285.75 ns | 97.75 ns | 5.358 ns | 168 B |
| Mockolate | 97.13 ns | 99.25 ns | 5.440 ns | 60 B |
| Moq | 514.02 ns | 74.25 ns | 4.070 ns | 296 B |
| NSubstitute | 601.32 ns | 166.86 ns | 9.146 ns | 272 B |
| FakeItEasy | 1,503.70 ns | 106.49 ns | 5.837 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1805
  bar [165.63, 285.75, 97.13, 514.02, 601.32, 1503.7]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,709.70 ns | 9,762.66 ns | 535.125 ns | 12736 B |
| Imposter | 29,107.61 ns | 4,931.72 ns | 270.324 ns | 16800 B |
| Mockolate | 10,542.20 ns | 5,476.53 ns | 300.187 ns | 8400 B |
| Moq | 76,246.07 ns | 6,397.02 ns | 350.642 ns | 37600 B |
| NSubstitute | 70,813.61 ns | 33,905.80 ns | 1,858.491 ns | 30848 B |
| FakeItEasy | 171,122.68 ns | 44,291.21 ns | 2,427.751 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 205348
  bar [26709.7, 29107.61, 10542.2, 76246.07, 70813.61, 171122.68]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-14T03:10:39.371Z*
