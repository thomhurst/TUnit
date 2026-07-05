---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 277.0 ns | 29.62 ns | 1.62 ns | 128 B |
| Imposter | 305.8 ns | 141.10 ns | 7.73 ns | 168 B |
| Mockolate | 126.6 ns | 200.09 ns | 10.97 ns | 84 B |
| Moq | 868.6 ns | 147.23 ns | 8.07 ns | 376 B |
| NSubstitute | 779.1 ns | 84.23 ns | 4.62 ns | 304 B |
| FakeItEasy | 1,887.8 ns | 576.02 ns | 31.57 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2266
  bar [277, 305.8, 126.6, 868.6, 779.1, 1887.8]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 174.9 ns | 128.06 ns | 7.02 ns | 96 B |
| Imposter | 303.6 ns | 75.68 ns | 4.15 ns | 168 B |
| Mockolate | 107.1 ns | 112.07 ns | 6.14 ns | 60 B |
| Moq | 583.7 ns | 172.49 ns | 9.45 ns | 296 B |
| NSubstitute | 641.4 ns | 505.35 ns | 27.70 ns | 272 B |
| FakeItEasy | 1,668.4 ns | 274.91 ns | 15.07 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2003
  bar [174.9, 303.6, 107.1, 583.7, 641.4, 1668.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,436.8 ns | 7,287.71 ns | 399.46 ns | 12736 B |
| Imposter | 29,919.3 ns | 9,155.64 ns | 501.85 ns | 16800 B |
| Mockolate | 12,357.5 ns | 11,080.04 ns | 607.33 ns | 8400 B |
| Moq | 79,939.7 ns | 16,463.47 ns | 902.42 ns | 37600 B |
| NSubstitute | 72,628.9 ns | 33,004.79 ns | 1,809.10 ns | 30848 B |
| FakeItEasy | 191,214.0 ns | 63,055.63 ns | 3,456.29 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 229457
  bar [27436.8, 29919.3, 12357.5, 79939.7, 72628.9, 191214]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-05T03:32:29.901Z*
