---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 759.45 ns | 11.066 ns | 10.351 ns | 2968 B |
| Imposter | 716.46 ns | 9.003 ns | 8.421 ns | 4688 B |
| Mockolate | 406.16 ns | 1.879 ns | 1.757 ns | 2240 B |
| Moq | 253,819.51 ns | 1,276.955 ns | 1,194.464 ns | 24324 B |
| NSubstitute | 5,999.58 ns | 51.491 ns | 48.165 ns | 10064 B |
| FakeItEasy | 6,771.57 ns | 54.300 ns | 48.136 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 304584
  bar [759.45, 716.46, 406.16, 253819.51, 5999.58, 6771.57]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.32 ns | 0.442 ns | 0.413 ns | 304 B |
| Imposter | 332.98 ns | 4.357 ns | 4.075 ns | 2400 B |
| Mockolate | 252.55 ns | 1.878 ns | 1.665 ns | 1240 B |
| Moq | 64,938.19 ns | 459.985 ns | 430.271 ns | 6925 B |
| NSubstitute | 3,500.07 ns | 26.176 ns | 24.485 ns | 7088 B |
| FakeItEasy | 3,321.23 ns | 60.490 ns | 56.582 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 77926
  bar [52.32, 332.98, 252.55, 64938.19, 3500.07, 3321.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,299.33 ns | 20.126 ns | 18.826 ns | 4384 B |
| Imposter | 1,832.18 ns | 28.726 ns | 25.465 ns | 11192 B |
| Mockolate | 1,125.00 ns | 22.131 ns | 23.680 ns | 5376 B |
| Moq | 351,200.76 ns | 2,807.932 ns | 2,344.750 ns | 34699 B |
| NSubstitute | 10,591.45 ns | 44.059 ns | 39.057 ns | 16889 B |
| FakeItEasy | 11,465.10 ns | 71.682 ns | 67.052 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 421441
  bar [1299.33, 1832.18, 1125, 351200.76, 10591.45, 11465.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-21T03:28:27.059Z*
