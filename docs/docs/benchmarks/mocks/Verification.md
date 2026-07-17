---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 745.25 ns | 11.338 ns | 10.051 ns | 3008 B |
| Imposter | 707.00 ns | 14.139 ns | 31.034 ns | 4688 B |
| Mockolate | 414.70 ns | 8.292 ns | 11.070 ns | 2128 B |
| Moq | 352,228.21 ns | 1,987.909 ns | 1,762.230 ns | 24325 B |
| NSubstitute | 6,649.92 ns | 46.103 ns | 40.869 ns | 10064 B |
| FakeItEasy | 7,861.88 ns | 57.637 ns | 48.130 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 422674
  bar [745.25, 707, 414.7, 352228.21, 6649.92, 7861.88]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.02 ns | 1.165 ns | 1.145 ns | 320 B |
| Imposter | 388.59 ns | 6.761 ns | 6.324 ns | 2400 B |
| Mockolate | 265.00 ns | 2.267 ns | 2.010 ns | 1144 B |
| Moq | 88,922.07 ns | 232.271 ns | 205.902 ns | 6918 B |
| NSubstitute | 3,710.28 ns | 26.917 ns | 25.178 ns | 7088 B |
| FakeItEasy | 3,893.22 ns | 33.859 ns | 31.672 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106707
  bar [60.02, 388.59, 265, 88922.07, 3710.28, 3893.22]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,385.20 ns | 18.228 ns | 17.050 ns | 4472 B |
| Imposter | 2,005.28 ns | 24.760 ns | 21.949 ns | 11192 B |
| Mockolate | 1,187.08 ns | 23.685 ns | 48.382 ns | 5240 B |
| Moq | 475,632.96 ns | 3,960.335 ns | 3,704.499 ns | 34699 B |
| NSubstitute | 11,960.08 ns | 66.488 ns | 62.193 ns | 16762 B |
| FakeItEasy | 14,727.81 ns | 95.668 ns | 89.488 ns | 19457 B |

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
  y-axis "Time (ns)" 0 --> 570760
  bar [1385.2, 2005.28, 1187.08, 475632.96, 11960.08, 14727.81]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-17T03:20:48.806Z*
