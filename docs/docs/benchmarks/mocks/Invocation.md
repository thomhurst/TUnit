---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.2 ns | 71.48 ns | 3.92 ns | 128 B |
| Imposter | 305.8 ns | 69.02 ns | 3.78 ns | 168 B |
| Mockolate | 119.2 ns | 26.46 ns | 1.45 ns | 84 B |
| Moq | 847.0 ns | 223.21 ns | 12.24 ns | 376 B |
| NSubstitute | 759.6 ns | 182.95 ns | 10.03 ns | 304 B |
| FakeItEasy | 1,850.9 ns | 423.13 ns | 23.19 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2222
  bar [281.2, 305.8, 119.2, 847, 759.6, 1850.9]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.2 ns | 74.29 ns | 4.07 ns | 96 B |
| Imposter | 305.1 ns | 93.77 ns | 5.14 ns | 168 B |
| Mockolate | 104.5 ns | 32.90 ns | 1.80 ns | 60 B |
| Moq | 574.6 ns | 100.31 ns | 5.50 ns | 296 B |
| NSubstitute | 654.2 ns | 203.06 ns | 11.13 ns | 272 B |
| FakeItEasy | 1,694.4 ns | 649.37 ns | 35.59 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2034
  bar [166.2, 305.1, 104.5, 574.6, 654.2, 1694.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,629.1 ns | 10,539.37 ns | 577.70 ns | 12736 B |
| Imposter | 29,783.4 ns | 6,618.80 ns | 362.80 ns | 16800 B |
| Mockolate | 11,591.1 ns | 6,263.54 ns | 343.33 ns | 8400 B |
| Moq | 84,112.4 ns | 9,011.68 ns | 493.96 ns | 37600 B |
| NSubstitute | 75,896.0 ns | 39,548.77 ns | 2,167.80 ns | 30848 B |
| FakeItEasy | 188,273.1 ns | 69,217.55 ns | 3,794.05 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 225928
  bar [27629.1, 29783.4, 11591.1, 84112.4, 75896, 188273.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-16T02:32:43.042Z*
