---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.6 ns | 81.56 ns | 4.47 ns | 128 B |
| Imposter | 287.6 ns | 68.11 ns | 3.73 ns | 168 B |
| Mockolate | 105.6 ns | 24.84 ns | 1.36 ns | 84 B |
| Moq | 806.9 ns | 138.77 ns | 7.61 ns | 376 B |
| NSubstitute | 701.7 ns | 317.11 ns | 17.38 ns | 304 B |
| FakeItEasy | 1,774.9 ns | 146.49 ns | 8.03 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2130
  bar [269.6, 287.6, 105.6, 806.9, 701.7, 1774.9]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.1 ns | 69.64 ns | 3.82 ns | 96 B |
| Imposter | 292.8 ns | 98.74 ns | 5.41 ns | 168 B |
| Mockolate | 103.1 ns | 73.73 ns | 4.04 ns | 60 B |
| Moq | 549.6 ns | 147.60 ns | 8.09 ns | 296 B |
| NSubstitute | 633.1 ns | 89.72 ns | 4.92 ns | 328 B |
| FakeItEasy | 1,534.8 ns | 168.33 ns | 9.23 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1842
  bar [164.1, 292.8, 103.1, 549.6, 633.1, 1534.8]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,828.9 ns | 9,506.45 ns | 521.08 ns | 12736 B |
| Imposter | 28,943.3 ns | 12,482.44 ns | 684.20 ns | 16800 B |
| Mockolate | 10,737.2 ns | 2,996.31 ns | 164.24 ns | 8400 B |
| Moq | 79,928.9 ns | 23,252.74 ns | 1,274.56 ns | 37600 B |
| NSubstitute | 77,348.3 ns | 24,082.17 ns | 1,320.03 ns | 36448 B |
| FakeItEasy | 173,590.9 ns | 41,040.97 ns | 2,249.59 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208310
  bar [26828.9, 28943.3, 10737.2, 79928.9, 77348.3, 173590.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-01T03:29:08.803Z*
