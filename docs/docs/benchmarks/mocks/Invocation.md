---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.403
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 255.93 ns | 295.651 ns | 16.206 ns | 128 B |
| Imposter | 143.12 ns | 13.287 ns | 0.728 ns | 96 B |
| Mockolate | 105.31 ns | 159.493 ns | 8.742 ns | 84 B |
| Moq | 703.70 ns | 326.904 ns | 17.919 ns | 376 B |
| NSubstitute | 680.81 ns | 898.767 ns | 49.264 ns | 304 B |
| FakeItEasy | 1,660.31 ns | 637.949 ns | 34.968 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1993
  bar [255.93, 143.12, 105.31, 703.7, 680.81, 1660.31]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 153.48 ns | 279.476 ns | 15.319 ns | 96 B |
| Imposter | 140.88 ns | 2.956 ns | 0.162 ns | 96 B |
| Mockolate | 90.90 ns | 56.350 ns | 3.089 ns | 60 B |
| Moq | 465.34 ns | 384.795 ns | 21.092 ns | 296 B |
| NSubstitute | 514.45 ns | 69.778 ns | 3.825 ns | 272 B |
| FakeItEasy | 1,490.86 ns | 1,843.406 ns | 101.043 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1790
  bar [153.48, 140.88, 90.9, 465.34, 514.45, 1490.86]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 23,908.41 ns | 23,434.277 ns | 1,284.512 ns | 12736 B |
| Imposter | 13,966.59 ns | 537.055 ns | 29.438 ns | 9600 B |
| Mockolate | 9,831.91 ns | 2,598.075 ns | 142.409 ns | 8400 B |
| Moq | 67,726.14 ns | 19,484.792 ns | 1,068.027 ns | 37600 B |
| NSubstitute | 69,037.21 ns | 14,209.770 ns | 778.885 ns | 36448 B |
| FakeItEasy | 161,688.92 ns | 48,832.565 ns | 2,676.678 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 194027
  bar [23908.41, 13966.59, 9831.91, 67726.14, 69037.21, 161688.92]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-01T02:27:32.250Z*
