---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.55 ns | 153.18 ns | 8.396 ns | 128 B |
| Imposter | 291.17 ns | 99.88 ns | 5.475 ns | 168 B |
| Mockolate | 98.13 ns | 11.37 ns | 0.623 ns | 84 B |
| Moq | 783.41 ns | 89.03 ns | 4.880 ns | 376 B |
| NSubstitute | 727.26 ns | 165.57 ns | 9.075 ns | 304 B |
| FakeItEasy | 1,804.38 ns | 834.50 ns | 45.742 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2166
  bar [269.55, 291.17, 98.13, 783.41, 727.26, 1804.38]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.25 ns | 77.18 ns | 4.231 ns | 96 B |
| Imposter | 294.79 ns | 90.05 ns | 4.936 ns | 168 B |
| Mockolate | 90.43 ns | 41.40 ns | 2.269 ns | 60 B |
| Moq | 506.91 ns | 186.09 ns | 10.200 ns | 296 B |
| NSubstitute | 611.78 ns | 239.35 ns | 13.119 ns | 272 B |
| FakeItEasy | 1,602.83 ns | 115.34 ns | 6.322 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1924
  bar [166.25, 294.79, 90.43, 506.91, 611.78, 1602.83]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,433.56 ns | 7,293.43 ns | 399.778 ns | 12736 B |
| Imposter | 28,740.27 ns | 6,062.18 ns | 332.288 ns | 16800 B |
| Mockolate | 9,707.59 ns | 1,524.64 ns | 83.571 ns | 8400 B |
| Moq | 79,127.19 ns | 25,511.88 ns | 1,398.392 ns | 37600 B |
| NSubstitute | 67,952.24 ns | 21,379.46 ns | 1,171.881 ns | 30848 B |
| FakeItEasy | 164,832.53 ns | 74,548.72 ns | 4,086.267 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 197800
  bar [26433.56, 28740.27, 9707.59, 79127.19, 67952.24, 164832.53]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-04T03:22:20.303Z*
