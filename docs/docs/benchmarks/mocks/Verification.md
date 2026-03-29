---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,919.3 ns | 58.56 ns | 172.68 ns | 4197 B |
| Imposter | 687.4 ns | 13.46 ns | 20.14 ns | 4688 B |
| Mockolate | 919.8 ns | 5.13 ns | 4.55 ns | 3168 B |
| Moq | 342,083.0 ns | 2,152.65 ns | 2,013.59 ns | 24325 B |
| NSubstitute | 6,209.9 ns | 65.08 ns | 54.34 ns | 10064 B |
| FakeItEasy | 7,571.8 ns | 78.48 ns | 73.41 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 410500
  bar [1919.3, 687.4, 919.8, 342083, 6209.9, 7571.8]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,153.0 ns | 35.91 ns | 105.87 ns | 1623 B |
| Imposter | 304.0 ns | 0.87 ns | 0.81 ns | 2400 B |
| Mockolate | 202.1 ns | 1.66 ns | 1.39 ns | 904 B |
| Moq | 86,458.8 ns | 436.51 ns | 408.31 ns | 6918 B |
| NSubstitute | 3,463.7 ns | 8.49 ns | 7.09 ns | 7088 B |
| FakeItEasy | 3,388.9 ns | 60.59 ns | 53.71 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103751
  bar [1153, 304, 202.1, 86458.8, 3463.7, 3388.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 2,322.6 ns | 25.36 ns | 21.17 ns | 6424 B |
| Imposter | 1,745.9 ns | 7.64 ns | 6.77 ns | 11192 B |
| Mockolate | 1,801.7 ns | 6.94 ns | 6.15 ns | 5592 B |
| Moq | 498,056.0 ns | 2,301.76 ns | 2,040.45 ns | 34779 B |
| NSubstitute | 11,270.9 ns | 222.29 ns | 207.93 ns | 16763 B |
| FakeItEasy | 12,705.2 ns | 126.41 ns | 105.56 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 597668
  bar [2322.6, 1745.9, 1801.7, 498056, 11270.9, 12705.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-29T22:20:59.126Z*
