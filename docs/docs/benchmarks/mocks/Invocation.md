---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 285.2 ns | 39.59 ns | 2.17 ns | 128 B |
| Imposter | 312.9 ns | 66.01 ns | 3.62 ns | 168 B |
| Mockolate | 132.4 ns | 159.90 ns | 8.76 ns | 84 B |
| Moq | 850.9 ns | 230.56 ns | 12.64 ns | 376 B |
| NSubstitute | 770.0 ns | 638.35 ns | 34.99 ns | 304 B |
| FakeItEasy | 1,838.9 ns | 574.67 ns | 31.50 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2207
  bar [285.2, 312.9, 132.4, 850.9, 770, 1838.9]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 174.9 ns | 68.57 ns | 3.76 ns | 96 B |
| Imposter | 319.0 ns | 243.51 ns | 13.35 ns | 168 B |
| Mockolate | 110.4 ns | 38.73 ns | 2.12 ns | 60 B |
| Moq | 573.5 ns | 141.79 ns | 7.77 ns | 296 B |
| NSubstitute | 627.4 ns | 235.22 ns | 12.89 ns | 272 B |
| FakeItEasy | 1,685.4 ns | 473.45 ns | 25.95 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2023
  bar [174.9, 319, 110.4, 573.5, 627.4, 1685.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,727.5 ns | 8,795.69 ns | 482.12 ns | 12736 B |
| Imposter | 31,440.0 ns | 25,607.66 ns | 1,403.64 ns | 16800 B |
| Mockolate | 12,764.4 ns | 2,876.31 ns | 157.66 ns | 8400 B |
| Moq | 84,451.0 ns | 15,280.99 ns | 837.60 ns | 37600 B |
| NSubstitute | 75,095.8 ns | 24,102.93 ns | 1,321.16 ns | 30848 B |
| FakeItEasy | 193,457.6 ns | 65,191.60 ns | 3,573.37 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 232150
  bar [27727.5, 31440, 12764.4, 84451, 75095.8, 193457.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-28T03:33:50.965Z*
