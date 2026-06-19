---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 274.8 ns | 88.68 ns | 4.86 ns | 128 B |
| Imposter | 299.9 ns | 66.81 ns | 3.66 ns | 168 B |
| Mockolate | 124.0 ns | 42.11 ns | 2.31 ns | 84 B |
| Moq | 848.4 ns | 162.83 ns | 8.93 ns | 376 B |
| NSubstitute | 738.4 ns | 103.83 ns | 5.69 ns | 304 B |
| FakeItEasy | 1,827.2 ns | 45.03 ns | 2.47 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2193
  bar [274.8, 299.9, 124, 848.4, 738.4, 1827.2]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.7 ns | 57.05 ns | 3.13 ns | 96 B |
| Imposter | 297.9 ns | 54.27 ns | 2.97 ns | 168 B |
| Mockolate | 105.1 ns | 73.09 ns | 4.01 ns | 60 B |
| Moq | 562.1 ns | 157.00 ns | 8.61 ns | 296 B |
| NSubstitute | 628.4 ns | 92.25 ns | 5.06 ns | 272 B |
| FakeItEasy | 1,609.9 ns | 350.39 ns | 19.21 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1932
  bar [166.7, 297.9, 105.1, 562.1, 628.4, 1609.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,582.9 ns | 16,334.68 ns | 895.36 ns | 12736 B |
| Imposter | 29,495.4 ns | 9,482.65 ns | 519.78 ns | 16800 B |
| Mockolate | 11,756.6 ns | 3,847.27 ns | 210.88 ns | 8400 B |
| Moq | 83,319.3 ns | 22,111.43 ns | 1,212.00 ns | 37600 B |
| NSubstitute | 73,136.5 ns | 9,854.58 ns | 540.16 ns | 30848 B |
| FakeItEasy | 187,548.1 ns | 30,365.87 ns | 1,664.46 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 225058
  bar [27582.9, 29495.4, 11756.6, 83319.3, 73136.5, 187548.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-19T03:29:43.427Z*
