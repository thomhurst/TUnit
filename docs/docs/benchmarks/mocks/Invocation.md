---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 279.0 ns | 145.95 ns | 8.00 ns | 128 B |
| Imposter | 305.1 ns | 114.84 ns | 6.29 ns | 168 B |
| Mockolate | 116.1 ns | 153.57 ns | 8.42 ns | 84 B |
| Moq | 870.5 ns | 233.33 ns | 12.79 ns | 376 B |
| NSubstitute | 778.1 ns | 359.21 ns | 19.69 ns | 304 B |
| FakeItEasy | 1,898.1 ns | 730.24 ns | 40.03 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2278
  bar [279, 305.1, 116.1, 870.5, 778.1, 1898.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.9 ns | 77.40 ns | 4.24 ns | 96 B |
| Imposter | 309.1 ns | 64.94 ns | 3.56 ns | 168 B |
| Mockolate | 110.7 ns | 84.55 ns | 4.63 ns | 60 B |
| Moq | 596.6 ns | 47.26 ns | 2.59 ns | 296 B |
| NSubstitute | 668.5 ns | 351.94 ns | 19.29 ns | 272 B |
| FakeItEasy | 1,699.0 ns | 623.61 ns | 34.18 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2039
  bar [165.9, 309.1, 110.7, 596.6, 668.5, 1699]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 31,592.8 ns | 15,214.97 ns | 833.98 ns | 13248 B |
| Imposter | 30,385.5 ns | 10,287.18 ns | 563.87 ns | 16800 B |
| Mockolate | 13,289.4 ns | 3,881.75 ns | 212.77 ns | 8400 B |
| Moq | 85,065.2 ns | 17,011.46 ns | 932.46 ns | 37600 B |
| NSubstitute | 83,541.6 ns | 17,312.08 ns | 948.93 ns | 36448 B |
| FakeItEasy | 200,912.3 ns | 143,824.53 ns | 7,883.51 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 241095
  bar [31592.8, 30385.5, 13289.4, 85065.2, 83541.6, 200912.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-25T02:41:00.074Z*
