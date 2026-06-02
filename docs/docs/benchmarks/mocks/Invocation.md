---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 261.40 ns | 51.31 ns | 2.813 ns | 120 B |
| Imposter | 303.32 ns | 83.69 ns | 4.587 ns | 168 B |
| Mockolate | 110.24 ns | 49.73 ns | 2.726 ns | 84 B |
| Moq | 853.69 ns | 56.86 ns | 3.117 ns | 376 B |
| NSubstitute | 746.11 ns | 188.51 ns | 10.333 ns | 304 B |
| FakeItEasy | 1,777.71 ns | 405.19 ns | 22.210 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2134
  bar [261.4, 303.32, 110.24, 853.69, 746.11, 1777.71]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.17 ns | 58.27 ns | 3.194 ns | 88 B |
| Imposter | 302.78 ns | 47.85 ns | 2.623 ns | 168 B |
| Mockolate | 98.11 ns | 45.36 ns | 2.486 ns | 60 B |
| Moq | 568.81 ns | 215.13 ns | 11.792 ns | 296 B |
| NSubstitute | 613.04 ns | 291.53 ns | 15.980 ns | 272 B |
| FakeItEasy | 1,620.27 ns | 307.86 ns | 16.875 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1945
  bar [165.17, 302.78, 98.11, 568.81, 613.04, 1620.27]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,558.13 ns | 11,041.86 ns | 605.242 ns | 11936 B |
| Imposter | 29,737.92 ns | 8,387.93 ns | 459.771 ns | 16800 B |
| Mockolate | 11,136.23 ns | 1,090.56 ns | 59.777 ns | 8400 B |
| Moq | 79,576.30 ns | 40,008.26 ns | 2,192.988 ns | 37600 B |
| NSubstitute | 78,645.57 ns | 19,332.98 ns | 1,059.706 ns | 36448 B |
| FakeItEasy | 183,611.89 ns | 35,815.82 ns | 1,963.186 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 220335
  bar [26558.13, 29737.92, 11136.23, 79576.3, 78645.57, 183611.89]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-02T03:30:24.417Z*
