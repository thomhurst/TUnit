---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 750.16 ns | 14.029 ns | 13.779 ns | 3008 B |
| Imposter | 732.14 ns | 9.140 ns | 8.549 ns | 4688 B |
| Mockolate | 414.69 ns | 5.132 ns | 4.549 ns | 2128 B |
| Moq | 344,307.90 ns | 1,979.879 ns | 1,755.111 ns | 24325 B |
| NSubstitute | 6,307.42 ns | 61.011 ns | 57.070 ns | 10064 B |
| FakeItEasy | 7,320.97 ns | 86.537 ns | 80.946 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 413170
  bar [750.16, 732.14, 414.69, 344307.9, 6307.42, 7320.97]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.20 ns | 0.475 ns | 0.421 ns | 320 B |
| Imposter | 313.08 ns | 3.683 ns | 3.445 ns | 2400 B |
| Mockolate | 220.88 ns | 1.767 ns | 1.653 ns | 1144 B |
| Moq | 88,479.87 ns | 363.589 ns | 322.312 ns | 6918 B |
| NSubstitute | 3,499.26 ns | 53.430 ns | 49.978 ns | 7088 B |
| FakeItEasy | 3,644.64 ns | 69.327 ns | 64.848 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106176
  bar [51.2, 313.08, 220.88, 88479.87, 3499.26, 3644.64]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,226.86 ns | 12.933 ns | 12.098 ns | 4472 B |
| Imposter | 1,776.76 ns | 24.979 ns | 23.366 ns | 11192 B |
| Mockolate | 1,085.32 ns | 9.172 ns | 8.580 ns | 5240 B |
| Moq | 481,754.37 ns | 3,213.842 ns | 3,006.229 ns | 34811 B |
| NSubstitute | 11,544.05 ns | 86.191 ns | 76.406 ns | 16889 B |
| FakeItEasy | 13,900.81 ns | 105.285 ns | 93.332 ns | 19345 B |

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
  y-axis "Time (ns)" 0 --> 578106
  bar [1226.86, 1776.76, 1085.32, 481754.37, 11544.05, 13900.81]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-06T03:43:04.080Z*
