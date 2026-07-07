---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 273.44 ns | 89.65 ns | 4.914 ns | 128 B |
| Imposter | 293.83 ns | 90.45 ns | 4.958 ns | 168 B |
| Mockolate | 105.93 ns | 15.62 ns | 0.856 ns | 84 B |
| Moq | 818.73 ns | 107.91 ns | 5.915 ns | 376 B |
| NSubstitute | 727.17 ns | 41.65 ns | 2.283 ns | 304 B |
| FakeItEasy | 1,793.85 ns | 772.88 ns | 42.364 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2153
  bar [273.44, 293.83, 105.93, 818.73, 727.17, 1793.85]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 163.43 ns | 71.32 ns | 3.909 ns | 96 B |
| Imposter | 294.80 ns | 45.08 ns | 2.471 ns | 168 B |
| Mockolate | 99.03 ns | 25.41 ns | 1.393 ns | 60 B |
| Moq | 535.92 ns | 143.16 ns | 7.847 ns | 296 B |
| NSubstitute | 639.60 ns | 196.66 ns | 10.780 ns | 272 B |
| FakeItEasy | 1,599.58 ns | 186.91 ns | 10.245 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1920
  bar [163.43, 294.8, 99.03, 535.92, 639.6, 1599.58]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,299.67 ns | 9,031.56 ns | 495.050 ns | 12736 B |
| Imposter | 28,978.46 ns | 7,397.13 ns | 405.461 ns | 16800 B |
| Mockolate | 10,636.28 ns | 307.00 ns | 16.828 ns | 8400 B |
| Moq | 80,452.28 ns | 19,798.25 ns | 1,085.209 ns | 37600 B |
| NSubstitute | 72,455.83 ns | 15,336.67 ns | 840.655 ns | 30848 B |
| FakeItEasy | 182,204.18 ns | 41,939.49 ns | 2,298.845 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 218646
  bar [27299.67, 28978.46, 10636.28, 80452.28, 72455.83, 182204.18]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-07T03:24:42.900Z*
