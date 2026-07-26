---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 284.1 ns | 96.41 ns | 5.28 ns | 128 B |
| Imposter | 309.7 ns | 62.68 ns | 3.44 ns | 168 B |
| Mockolate | 127.4 ns | 74.45 ns | 4.08 ns | 84 B |
| Moq | 867.2 ns | 131.03 ns | 7.18 ns | 376 B |
| NSubstitute | 756.1 ns | 305.63 ns | 16.75 ns | 304 B |
| FakeItEasy | 1,916.0 ns | 667.33 ns | 36.58 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2300
  bar [284.1, 309.7, 127.4, 867.2, 756.1, 1916]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 167.2 ns | 84.26 ns | 4.62 ns | 96 B |
| Imposter | 308.3 ns | 127.93 ns | 7.01 ns | 168 B |
| Mockolate | 105.8 ns | 38.03 ns | 2.08 ns | 60 B |
| Moq | 573.4 ns | 354.83 ns | 19.45 ns | 296 B |
| NSubstitute | 682.1 ns | 47.78 ns | 2.62 ns | 328 B |
| FakeItEasy | 1,725.8 ns | 522.34 ns | 28.63 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2071
  bar [167.2, 308.3, 105.8, 573.4, 682.1, 1725.8]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,961.6 ns | 12,870.35 ns | 705.47 ns | 12736 B |
| Imposter | 30,169.7 ns | 5,334.86 ns | 292.42 ns | 16800 B |
| Mockolate | 12,039.0 ns | 4,626.00 ns | 253.57 ns | 8400 B |
| Moq | 85,076.4 ns | 34,742.97 ns | 1,904.38 ns | 37600 B |
| NSubstitute | 74,735.0 ns | 12,722.77 ns | 697.38 ns | 30848 B |
| FakeItEasy | 196,430.6 ns | 65,680.05 ns | 3,600.15 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 235717
  bar [27961.6, 30169.7, 12039, 85076.4, 74735, 196430.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-26T03:33:46.478Z*
