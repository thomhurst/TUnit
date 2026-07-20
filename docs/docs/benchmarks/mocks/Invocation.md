---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 227.3 ns | 80.77 ns | 4.43 ns | 128 B |
| Imposter | 248.3 ns | 18.94 ns | 1.04 ns | 168 B |
| Mockolate | 123.8 ns | 52.90 ns | 2.90 ns | 84 B |
| Moq | 699.7 ns | 219.03 ns | 12.01 ns | 376 B |
| NSubstitute | 654.9 ns | 106.19 ns | 5.82 ns | 304 B |
| FakeItEasy | 1,663.7 ns | 477.48 ns | 26.17 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1997
  bar [227.3, 248.3, 123.8, 699.7, 654.9, 1663.7]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 137.2 ns | 56.47 ns | 3.10 ns | 96 B |
| Imposter | 248.3 ns | 22.36 ns | 1.23 ns | 168 B |
| Mockolate | 103.0 ns | 17.26 ns | 0.95 ns | 60 B |
| Moq | 492.4 ns | 392.61 ns | 21.52 ns | 296 B |
| NSubstitute | 526.3 ns | 248.63 ns | 13.63 ns | 272 B |
| FakeItEasy | 1,450.6 ns | 243.97 ns | 13.37 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1741
  bar [137.2, 248.3, 103, 492.4, 526.3, 1450.6]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 22,682.0 ns | 15,500.46 ns | 849.63 ns | 12736 B |
| Imposter | 24,648.6 ns | 2,618.30 ns | 143.52 ns | 16800 B |
| Mockolate | 12,109.3 ns | 1,859.86 ns | 101.95 ns | 8400 B |
| Moq | 69,056.1 ns | 7,160.88 ns | 392.51 ns | 37600 B |
| NSubstitute | 63,685.5 ns | 14,186.97 ns | 777.64 ns | 30848 B |
| FakeItEasy | 168,269.6 ns | 12,949.28 ns | 709.79 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 201924
  bar [22682, 24648.6, 12109.3, 69056.1, 63685.5, 168269.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-20T03:22:58.159Z*
