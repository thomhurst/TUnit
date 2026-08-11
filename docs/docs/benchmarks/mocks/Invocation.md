---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.7 ns | 215.23 ns | 11.80 ns | 128 B |
| Imposter | 307.0 ns | 123.12 ns | 6.75 ns | 168 B |
| Mockolate | 115.4 ns | 149.62 ns | 8.20 ns | 84 B |
| Moq | 885.1 ns | 644.47 ns | 35.33 ns | 376 B |
| NSubstitute | 815.8 ns | 166.85 ns | 9.15 ns | 304 B |
| FakeItEasy | 1,883.4 ns | 222.68 ns | 12.21 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2261
  bar [281.7, 307, 115.4, 885.1, 815.8, 1883.4]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 175.0 ns | 55.53 ns | 3.04 ns | 96 B |
| Imposter | 313.8 ns | 121.75 ns | 6.67 ns | 168 B |
| Mockolate | 102.7 ns | 64.46 ns | 3.53 ns | 60 B |
| Moq | 571.6 ns | 83.26 ns | 4.56 ns | 296 B |
| NSubstitute | 643.3 ns | 310.76 ns | 17.03 ns | 272 B |
| FakeItEasy | 1,804.7 ns | 626.86 ns | 34.36 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2166
  bar [175, 313.8, 102.7, 571.6, 643.3, 1804.7]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,317.4 ns | 11,462.18 ns | 628.28 ns | 12736 B |
| Imposter | 30,708.3 ns | 8,690.78 ns | 476.37 ns | 16800 B |
| Mockolate | 14,309.2 ns | 6,451.33 ns | 353.62 ns | 8400 B |
| Moq | 87,054.1 ns | 20,546.55 ns | 1,126.23 ns | 37600 B |
| NSubstitute | 78,510.5 ns | 33,630.68 ns | 1,843.41 ns | 30848 B |
| FakeItEasy | 189,368.4 ns | 100,333.16 ns | 5,499.60 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 227243
  bar [28317.4, 30708.3, 14309.2, 87054.1, 78510.5, 189368.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-11T02:59:33.302Z*
