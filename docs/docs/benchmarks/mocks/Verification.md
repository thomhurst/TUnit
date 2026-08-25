---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 708.54 ns | 6.064 ns | 5.672 ns | 3008 B |
| Imposter | 732.60 ns | 6.621 ns | 6.193 ns | 4688 B |
| Mockolate | 395.11 ns | 2.843 ns | 2.659 ns | 2128 B |
| Moq | 343,509.42 ns | 1,959.325 ns | 1,832.754 ns | 24325 B |
| NSubstitute | 7,010.84 ns | 77.096 ns | 72.115 ns | 10064 B |
| FakeItEasy | 7,523.84 ns | 27.491 ns | 25.715 ns | 10722 B |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 412212
  bar [708.54, 732.6, 395.11, 343509.42, 7010.84, 7523.84]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.71 ns | 0.593 ns | 0.554 ns | 320 B |
| Imposter | 342.20 ns | 2.923 ns | 2.591 ns | 2400 B |
| Mockolate | 233.11 ns | 1.808 ns | 1.691 ns | 1144 B |
| Moq | 87,003.65 ns | 284.122 ns | 251.867 ns | 6918 B |
| NSubstitute | 3,958.83 ns | 20.776 ns | 19.434 ns | 7088 B |
| FakeItEasy | 3,589.01 ns | 41.458 ns | 36.751 ns | 5209 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 104405
  bar [52.71, 342.2, 233.11, 87003.65, 3958.83, 3589.01]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,265.24 ns | 6.463 ns | 6.046 ns | 4472 B |
| Imposter | 1,777.92 ns | 15.743 ns | 13.955 ns | 11192 B |
| Mockolate | 1,119.21 ns | 7.148 ns | 6.336 ns | 5240 B |
| Moq | 475,737.35 ns | 4,140.869 ns | 3,873.372 ns | 34699 B |
| NSubstitute | 12,617.34 ns | 112.751 ns | 105.467 ns | 16929 B |
| FakeItEasy | 13,356.53 ns | 91.050 ns | 76.031 ns | 19233 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 570885
  bar [1265.24, 1777.92, 1119.21, 475737.35, 12617.34, 13356.53]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-25T02:41:00.074Z*
