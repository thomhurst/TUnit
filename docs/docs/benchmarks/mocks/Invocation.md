---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.88 ns | 376.02 ns | 20.611 ns | 128 B |
| Imposter | 81.41 ns | 84.95 ns | 4.657 ns | 48 B |
| Mockolate | 134.01 ns | 110.70 ns | 6.068 ns | 84 B |
| Moq | 919.86 ns | 483.28 ns | 26.490 ns | 376 B |
| NSubstitute | 707.60 ns | 1,587.99 ns | 87.043 ns | 304 B |
| FakeItEasy | 2,235.24 ns | 2,538.10 ns | 139.122 ns | 960 B |

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
  y-axis "Time (ns)" 0 --> 2683
  bar [281.88, 81.41, 134.01, 919.86, 707.6, 2235.24]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 236.16 ns | 516.34 ns | 28.303 ns | 96 B |
| Imposter | 89.09 ns | 109.64 ns | 6.010 ns | 48 B |
| Mockolate | 124.35 ns | 147.82 ns | 8.102 ns | 60 B |
| Moq | 606.46 ns | 1,494.03 ns | 81.893 ns | 296 B |
| NSubstitute | 555.68 ns | 1,608.88 ns | 88.188 ns | 272 B |
| FakeItEasy | 1,837.95 ns | 234.94 ns | 12.878 ns | 800 B |

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
  y-axis "Time (ns)" 0 --> 2206
  bar [236.16, 89.09, 124.35, 606.46, 555.68, 1837.95]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 32,283.28 ns | 93,897.96 ns | 5,146.864 ns | 13248 B |
| Imposter | 8,344.06 ns | 6,713.62 ns | 367.996 ns | 4800 B |
| Mockolate | 13,252.88 ns | 9,800.23 ns | 537.183 ns | 8400 B |
| Moq | 84,411.47 ns | 125,852.53 ns | 6,898.402 ns | 37600 B |
| NSubstitute | 77,250.14 ns | 43,425.68 ns | 2,380.308 ns | 30848 B |
| FakeItEasy | 191,764.03 ns | 38,543.57 ns | 2,112.703 ns | 96000 B |

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
  y-axis "Time (ns)" 0 --> 230117
  bar [32283.28, 8344.06, 13252.88, 84411.47, 77250.14, 191764.03]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-16T02:25:25.447Z*
