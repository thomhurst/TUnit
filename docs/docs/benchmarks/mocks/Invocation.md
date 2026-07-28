---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 282.12 ns | 75.91 ns | 4.161 ns | 128 B |
| Imposter | 304.95 ns | 27.24 ns | 1.493 ns | 168 B |
| Mockolate | 118.74 ns | 24.02 ns | 1.317 ns | 84 B |
| Moq | 857.72 ns | 124.25 ns | 6.811 ns | 376 B |
| NSubstitute | 753.89 ns | 373.38 ns | 20.466 ns | 304 B |
| FakeItEasy | 1,796.47 ns | 552.41 ns | 30.279 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2156
  bar [282.12, 304.95, 118.74, 857.72, 753.89, 1796.47]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.14 ns | 73.27 ns | 4.016 ns | 96 B |
| Imposter | 302.18 ns | 70.66 ns | 3.873 ns | 168 B |
| Mockolate | 91.49 ns | 66.55 ns | 3.648 ns | 60 B |
| Moq | 523.17 ns | 50.16 ns | 2.749 ns | 296 B |
| NSubstitute | 612.28 ns | 364.78 ns | 19.995 ns | 272 B |
| FakeItEasy | 1,518.63 ns | 384.61 ns | 21.082 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1823
  bar [166.14, 302.18, 91.49, 523.17, 612.28, 1518.63]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,607.94 ns | 14,189.95 ns | 777.799 ns | 12736 B |
| Imposter | 29,556.93 ns | 6,515.32 ns | 357.127 ns | 16800 B |
| Mockolate | 10,839.91 ns | 2,106.63 ns | 115.471 ns | 8400 B |
| Moq | 83,335.71 ns | 5,030.17 ns | 275.721 ns | 37600 B |
| NSubstitute | 73,719.05 ns | 25,779.90 ns | 1,413.083 ns | 30848 B |
| FakeItEasy | 187,021.19 ns | 46,436.22 ns | 2,545.326 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 224426
  bar [27607.94, 29556.93, 10839.91, 83335.71, 73719.05, 187021.19]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-28T03:20:43.557Z*
