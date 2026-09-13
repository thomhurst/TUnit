---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 273.67 ns | 84.15 ns | 4.612 ns | 128 B |
| Imposter | 293.85 ns | 78.66 ns | 4.311 ns | 168 B |
| Mockolate | 108.82 ns | 52.13 ns | 2.858 ns | 84 B |
| Moq | 809.95 ns | 159.28 ns | 8.731 ns | 376 B |
| NSubstitute | 709.05 ns | 287.34 ns | 15.750 ns | 304 B |
| FakeItEasy | 1,732.57 ns | 272.54 ns | 14.939 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2080
  bar [273.67, 293.85, 108.82, 809.95, 709.05, 1732.57]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.67 ns | 74.65 ns | 4.092 ns | 96 B |
| Imposter | 302.82 ns | 76.64 ns | 4.201 ns | 168 B |
| Mockolate | 94.65 ns | 25.44 ns | 1.394 ns | 60 B |
| Moq | 548.76 ns | 332.55 ns | 18.228 ns | 296 B |
| NSubstitute | 618.12 ns | 142.38 ns | 7.804 ns | 272 B |
| FakeItEasy | 1,590.17 ns | 353.72 ns | 19.388 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1909
  bar [165.67, 302.82, 94.65, 548.76, 618.12, 1590.17]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,414.73 ns | 10,855.61 ns | 595.033 ns | 12736 B |
| Imposter | 29,509.99 ns | 15,435.84 ns | 846.090 ns | 16800 B |
| Mockolate | 10,866.83 ns | 5,406.29 ns | 296.337 ns | 8400 B |
| Moq | 84,203.67 ns | 17,807.72 ns | 976.101 ns | 37600 B |
| NSubstitute | 71,268.34 ns | 21,734.81 ns | 1,191.358 ns | 30848 B |
| FakeItEasy | 180,944.79 ns | 81,750.74 ns | 4,481.034 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 217134
  bar [27414.73, 29509.99, 10866.83, 84203.67, 71268.34, 180944.79]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-13T02:33:28.296Z*
