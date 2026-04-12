---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 252.1 ns | 28.86 ns | 1.58 ns | 120 B |
| Imposter | 280.5 ns | 96.04 ns | 5.26 ns | 168 B |
| Mockolate | 622.0 ns | 140.74 ns | 7.71 ns | 640 B |
| Moq | 762.4 ns | 50.56 ns | 2.77 ns | 376 B |
| NSubstitute | 721.0 ns | 65.79 ns | 3.61 ns | 360 B |
| FakeItEasy | 1,606.8 ns | 306.55 ns | 16.80 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1929
  bar [252.1, 280.5, 622, 762.4, 721, 1606.8]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 153.3 ns | 63.61 ns | 3.49 ns | 88 B |
| Imposter | 279.8 ns | 95.70 ns | 5.25 ns | 168 B |
| Mockolate | 497.8 ns | 162.93 ns | 8.93 ns | 520 B |
| Moq | 492.6 ns | 122.34 ns | 6.71 ns | 296 B |
| NSubstitute | 610.0 ns | 96.85 ns | 5.31 ns | 328 B |
| FakeItEasy | 1,413.0 ns | 352.98 ns | 19.35 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1696
  bar [153.3, 279.8, 497.8, 492.6, 610, 1413]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,358.4 ns | 7,197.36 ns | 394.51 ns | 11936 B |
| Imposter | 28,108.6 ns | 6,895.80 ns | 377.98 ns | 16800 B |
| Mockolate | 62,708.5 ns | 22,552.50 ns | 1,236.18 ns | 64000 B |
| Moq | 75,398.7 ns | 6,366.67 ns | 348.98 ns | 37600 B |
| NSubstitute | 75,682.6 ns | 39,055.23 ns | 2,140.75 ns | 36448 B |
| FakeItEasy | 162,987.7 ns | 37,019.36 ns | 2,029.16 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 195586
  bar [25358.4, 28108.6, 62708.5, 75398.7, 75682.6, 162987.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-12T03:28:39.462Z*
