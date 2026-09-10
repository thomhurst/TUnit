---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.10 ns | 43.65 ns | 2.393 ns | 128 B |
| Imposter | 288.28 ns | 60.26 ns | 3.303 ns | 168 B |
| Mockolate | 97.12 ns | 24.99 ns | 1.370 ns | 84 B |
| Moq | 772.70 ns | 192.98 ns | 10.578 ns | 376 B |
| NSubstitute | 674.66 ns | 157.29 ns | 8.621 ns | 304 B |
| FakeItEasy | 1,612.88 ns | 248.04 ns | 13.596 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1936
  bar [269.1, 288.28, 97.12, 772.7, 674.66, 1612.88]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 167.17 ns | 62.99 ns | 3.453 ns | 96 B |
| Imposter | 293.44 ns | 100.74 ns | 5.522 ns | 168 B |
| Mockolate | 89.72 ns | 13.44 ns | 0.737 ns | 60 B |
| Moq | 511.92 ns | 143.01 ns | 7.839 ns | 296 B |
| NSubstitute | 587.19 ns | 167.15 ns | 9.162 ns | 272 B |
| FakeItEasy | 1,466.12 ns | 108.76 ns | 5.962 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1760
  bar [167.17, 293.44, 89.72, 511.92, 587.19, 1466.12]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,710.31 ns | 8,718.45 ns | 477.888 ns | 12736 B |
| Imposter | 28,496.53 ns | 10,044.83 ns | 550.591 ns | 16800 B |
| Mockolate | 9,726.67 ns | 2,244.99 ns | 123.056 ns | 8400 B |
| Moq | 77,431.14 ns | 17,104.16 ns | 937.537 ns | 37600 B |
| NSubstitute | 68,544.95 ns | 29,665.51 ns | 1,626.067 ns | 30848 B |
| FakeItEasy | 171,307.50 ns | 38,179.34 ns | 2,092.739 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 205569
  bar [26710.31, 28496.53, 9726.67, 77431.14, 68544.95, 171307.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-09T02:32:56.707Z*
