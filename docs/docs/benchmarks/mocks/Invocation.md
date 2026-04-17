---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 263.5 ns | 74.99 ns | 4.11 ns | 120 B |
| Imposter | 303.6 ns | 71.00 ns | 3.89 ns | 168 B |
| Mockolate | 687.7 ns | 80.12 ns | 4.39 ns | 640 B |
| Moq | 801.1 ns | 191.96 ns | 10.52 ns | 376 B |
| NSubstitute | 743.6 ns | 122.94 ns | 6.74 ns | 304 B |
| FakeItEasy | 1,772.1 ns | 397.78 ns | 21.80 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2127
  bar [263.5, 303.6, 687.7, 801.1, 743.6, 1772.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.1 ns | 71.02 ns | 3.89 ns | 88 B |
| Imposter | 306.7 ns | 121.47 ns | 6.66 ns | 168 B |
| Mockolate | 543.0 ns | 90.95 ns | 4.99 ns | 520 B |
| Moq | 536.6 ns | 100.52 ns | 5.51 ns | 296 B |
| NSubstitute | 653.8 ns | 679.02 ns | 37.22 ns | 272 B |
| FakeItEasy | 1,619.8 ns | 547.84 ns | 30.03 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1944
  bar [166.1, 306.7, 543, 536.6, 653.8, 1619.8]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,494.2 ns | 13,807.64 ns | 756.84 ns | 11936 B |
| Imposter | 30,093.3 ns | 8,247.06 ns | 452.05 ns | 16800 B |
| Mockolate | 70,940.2 ns | 18,268.87 ns | 1,001.38 ns | 64000 B |
| Moq | 82,141.8 ns | 5,729.62 ns | 314.06 ns | 37600 B |
| NSubstitute | 73,823.5 ns | 5,831.24 ns | 319.63 ns | 30848 B |
| FakeItEasy | 186,702.0 ns | 247,385.84 ns | 13,560.05 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 224043
  bar [26494.2, 30093.3, 70940.2, 82141.8, 73823.5, 186702]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-17T03:23:50.633Z*
