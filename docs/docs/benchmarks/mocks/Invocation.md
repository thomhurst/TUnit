---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.95 ns | 93.227 ns | 5.110 ns | 128 B |
| Imposter | 299.20 ns | 182.182 ns | 9.986 ns | 168 B |
| Mockolate | 106.83 ns | 9.094 ns | 0.498 ns | 84 B |
| Moq | 808.65 ns | 37.365 ns | 2.048 ns | 376 B |
| NSubstitute | 777.82 ns | 117.337 ns | 6.432 ns | 360 B |
| FakeItEasy | 1,749.31 ns | 226.142 ns | 12.396 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2100
  bar [269.95, 299.2, 106.83, 808.65, 777.82, 1749.31]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.10 ns | 79.083 ns | 4.335 ns | 96 B |
| Imposter | 290.95 ns | 55.317 ns | 3.032 ns | 168 B |
| Mockolate | 96.79 ns | 31.480 ns | 1.726 ns | 60 B |
| Moq | 537.42 ns | 139.463 ns | 7.644 ns | 296 B |
| NSubstitute | 585.36 ns | 220.250 ns | 12.073 ns | 272 B |
| FakeItEasy | 1,463.68 ns | 360.436 ns | 19.757 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1757
  bar [166.1, 290.95, 96.79, 537.42, 585.36, 1463.68]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,487.88 ns | 8,778.198 ns | 481.163 ns | 12736 B |
| Imposter | 28,046.98 ns | 5,604.410 ns | 307.197 ns | 16800 B |
| Mockolate | 10,844.64 ns | 10,898.301 ns | 597.373 ns | 8400 B |
| Moq | 77,618.64 ns | 6,181.350 ns | 338.821 ns | 37600 B |
| NSubstitute | 72,996.64 ns | 10,873.652 ns | 596.022 ns | 36448 B |
| FakeItEasy | 167,309.91 ns | 39,335.875 ns | 2,156.132 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 200772
  bar [26487.88, 28046.98, 10844.64, 77618.64, 72996.64, 167309.91]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-17T03:28:53.706Z*
