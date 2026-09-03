---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 212.46 ns | 61.409 ns | 3.366 ns | 128 B |
| Imposter | 232.93 ns | 12.898 ns | 0.707 ns | 168 B |
| Mockolate | 85.08 ns | 13.836 ns | 0.758 ns | 84 B |
| Moq | 604.40 ns | 156.271 ns | 8.566 ns | 376 B |
| NSubstitute | 571.19 ns | 122.537 ns | 6.717 ns | 304 B |
| FakeItEasy | 1,377.65 ns | 10.460 ns | 0.573 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1654
  bar [212.46, 232.93, 85.08, 604.4, 571.19, 1377.65]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 136.38 ns | 52.183 ns | 2.860 ns | 96 B |
| Imposter | 234.68 ns | 10.407 ns | 0.570 ns | 168 B |
| Mockolate | 78.21 ns | 8.341 ns | 0.457 ns | 60 B |
| Moq | 415.86 ns | 60.838 ns | 3.335 ns | 296 B |
| NSubstitute | 475.04 ns | 130.333 ns | 7.144 ns | 272 B |
| FakeItEasy | 1,243.63 ns | 139.049 ns | 7.622 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1493
  bar [136.38, 234.68, 78.21, 415.86, 475.04, 1243.63]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 21,171.91 ns | 7,626.921 ns | 418.057 ns | 12736 B |
| Imposter | 23,239.12 ns | 761.537 ns | 41.742 ns | 16800 B |
| Mockolate | 8,449.66 ns | 2,210.805 ns | 121.182 ns | 8400 B |
| Moq | 62,304.89 ns | 6,907.633 ns | 378.631 ns | 37600 B |
| NSubstitute | 59,492.28 ns | 2,134.354 ns | 116.991 ns | 36448 B |
| FakeItEasy | 139,160.01 ns | 43,614.957 ns | 2,390.683 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 166993
  bar [21171.91, 23239.12, 8449.66, 62304.89, 59492.28, 139160.01]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-03T02:45:05.205Z*
