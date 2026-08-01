---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.7 ns | 80.73 ns | 4.43 ns | 128 B |
| Imposter | 313.8 ns | 78.04 ns | 4.28 ns | 168 B |
| Mockolate | 136.8 ns | 30.69 ns | 1.68 ns | 84 B |
| Moq | 880.1 ns | 211.19 ns | 11.58 ns | 376 B |
| NSubstitute | 813.0 ns | 248.49 ns | 13.62 ns | 304 B |
| FakeItEasy | 2,003.4 ns | 659.37 ns | 36.14 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2405
  bar [281.7, 313.8, 136.8, 880.1, 813, 2003.4]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 175.6 ns | 61.54 ns | 3.37 ns | 96 B |
| Imposter | 327.6 ns | 423.24 ns | 23.20 ns | 168 B |
| Mockolate | 116.1 ns | 78.74 ns | 4.32 ns | 60 B |
| Moq | 593.5 ns | 114.38 ns | 6.27 ns | 296 B |
| NSubstitute | 645.6 ns | 230.75 ns | 12.65 ns | 272 B |
| FakeItEasy | 1,838.6 ns | 1,204.30 ns | 66.01 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2207
  bar [175.6, 327.6, 116.1, 593.5, 645.6, 1838.6]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,081.7 ns | 10,519.38 ns | 576.60 ns | 12736 B |
| Imposter | 32,043.1 ns | 29,070.23 ns | 1,593.44 ns | 16800 B |
| Mockolate | 13,823.5 ns | 2,330.46 ns | 127.74 ns | 8400 B |
| Moq | 86,318.8 ns | 24,085.61 ns | 1,320.21 ns | 37600 B |
| NSubstitute | 77,995.4 ns | 24,362.96 ns | 1,335.42 ns | 30848 B |
| FakeItEasy | 199,337.1 ns | 62,850.71 ns | 3,445.06 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 239205
  bar [28081.7, 32043.1, 13823.5, 86318.8, 77995.4, 199337.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-01T03:21:53.196Z*
