---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 272.6 ns | 56.40 ns | 3.09 ns | 128 B |
| Imposter | 368.7 ns | 38.27 ns | 2.10 ns | 168 B |
| Mockolate | 121.8 ns | 61.95 ns | 3.40 ns | 84 B |
| Moq | 814.8 ns | 350.95 ns | 19.24 ns | 376 B |
| NSubstitute | 820.6 ns | 1,031.26 ns | 56.53 ns | 304 B |
| FakeItEasy | 1,852.5 ns | 1,311.34 ns | 71.88 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2223
  bar [272.6, 368.7, 121.8, 814.8, 820.6, 1852.5]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 174.2 ns | 56.08 ns | 3.07 ns | 96 B |
| Imposter | 302.7 ns | 87.58 ns | 4.80 ns | 168 B |
| Mockolate | 106.0 ns | 194.26 ns | 10.65 ns | 60 B |
| Moq | 550.0 ns | 101.00 ns | 5.54 ns | 296 B |
| NSubstitute | 641.9 ns | 53.00 ns | 2.90 ns | 272 B |
| FakeItEasy | 1,677.0 ns | 64.46 ns | 3.53 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2013
  bar [174.2, 302.7, 106, 550, 641.9, 1677]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,979.5 ns | 6,941.53 ns | 380.49 ns | 12736 B |
| Imposter | 33,056.6 ns | 13,945.35 ns | 764.39 ns | 16800 B |
| Mockolate | 13,212.4 ns | 20,439.77 ns | 1,120.37 ns | 8400 B |
| Moq | 84,896.3 ns | 58,242.67 ns | 3,192.48 ns | 37600 B |
| NSubstitute | 87,247.2 ns | 62,681.56 ns | 3,435.79 ns | 30848 B |
| FakeItEasy | 206,938.9 ns | 213,933.73 ns | 11,726.43 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 248327
  bar [27979.5, 33056.6, 13212.4, 84896.3, 87247.2, 206938.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-18T03:29:53.480Z*
