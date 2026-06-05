---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 215.31 ns | 56.957 ns | 3.122 ns | 128 B |
| Imposter | 232.80 ns | 4.920 ns | 0.270 ns | 168 B |
| Mockolate | 88.80 ns | 33.625 ns | 1.843 ns | 84 B |
| Moq | 641.20 ns | 425.362 ns | 23.316 ns | 376 B |
| NSubstitute | 607.70 ns | 516.136 ns | 28.291 ns | 304 B |
| FakeItEasy | 1,605.23 ns | 656.898 ns | 36.007 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1927
  bar [215.31, 232.8, 88.8, 641.2, 607.7, 1605.23]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 134.65 ns | 76.457 ns | 4.191 ns | 96 B |
| Imposter | 241.29 ns | 186.342 ns | 10.214 ns | 168 B |
| Mockolate | 83.27 ns | 40.791 ns | 2.236 ns | 60 B |
| Moq | 425.75 ns | 230.811 ns | 12.652 ns | 296 B |
| NSubstitute | 474.66 ns | 19.305 ns | 1.058 ns | 272 B |
| FakeItEasy | 1,321.39 ns | 957.700 ns | 52.495 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1586
  bar [134.65, 241.29, 83.27, 425.75, 474.66, 1321.39]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 21,480.79 ns | 9,465.427 ns | 518.832 ns | 12736 B |
| Imposter | 23,277.23 ns | 4,438.146 ns | 243.270 ns | 16800 B |
| Mockolate | 9,136.16 ns | 2,795.490 ns | 153.230 ns | 8400 B |
| Moq | 62,248.77 ns | 5,991.129 ns | 328.394 ns | 37600 B |
| NSubstitute | 63,845.01 ns | 16,346.905 ns | 896.029 ns | 30848 B |
| FakeItEasy | 145,137.77 ns | 63,263.258 ns | 3,467.673 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 174166
  bar [21480.79, 23277.23, 9136.16, 62248.77, 63845.01, 145137.77]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-05T03:30:04.148Z*
