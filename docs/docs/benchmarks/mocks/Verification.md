---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 766.30 ns | 4.647 ns | 4.347 ns | 3080 B |
| Imposter | 710.72 ns | 13.332 ns | 12.471 ns | 4688 B |
| Mockolate | 1,003.08 ns | 19.549 ns | 17.330 ns | 3152 B |
| Moq | 246,139.47 ns | 1,135.665 ns | 1,006.737 ns | 24324 B |
| NSubstitute | 6,085.89 ns | 83.154 ns | 77.782 ns | 10064 B |
| FakeItEasy | 6,790.79 ns | 77.529 ns | 72.521 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 295368
  bar [766.3, 710.72, 1003.08, 246139.47, 6085.89, 6790.79]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 70.84 ns | 0.384 ns | 0.300 ns | 328 B |
| Imposter | 340.38 ns | 4.252 ns | 3.551 ns | 2400 B |
| Mockolate | 238.11 ns | 2.148 ns | 2.010 ns | 952 B |
| Moq | 62,092.73 ns | 480.187 ns | 449.167 ns | 6925 B |
| NSubstitute | 3,532.46 ns | 28.050 ns | 24.865 ns | 7088 B |
| FakeItEasy | 3,390.19 ns | 32.098 ns | 26.803 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74512
  bar [70.84, 340.38, 238.11, 62092.73, 3532.46, 3390.19]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,390.15 ns | 12.953 ns | 12.116 ns | 4608 B |
| Imposter | 1,858.61 ns | 24.956 ns | 22.123 ns | 11192 B |
| Mockolate | 1,914.36 ns | 19.722 ns | 17.483 ns | 5496 B |
| Moq | 348,235.34 ns | 1,541.299 ns | 1,203.345 ns | 34699 B |
| NSubstitute | 10,910.92 ns | 65.888 ns | 58.408 ns | 16761 B |
| FakeItEasy | 12,227.38 ns | 61.714 ns | 51.534 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 417883
  bar [1390.15, 1858.61, 1914.36, 348235.34, 10910.92, 12227.38]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-11T03:20:45.459Z*
