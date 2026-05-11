---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 540.99 ns | 2.488 ns | 2.077 ns | 2864 B |
| Imposter | 530.76 ns | 2.776 ns | 2.596 ns | 4688 B |
| Mockolate | 356.76 ns | 3.379 ns | 3.161 ns | 2224 B |
| Moq | 192,477.33 ns | 2,001.374 ns | 1,872.087 ns | 24324 B |
| NSubstitute | 4,568.82 ns | 67.577 ns | 63.212 ns | 10064 B |
| FakeItEasy | 5,231.81 ns | 66.899 ns | 62.577 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 230973
  bar [540.99, 530.76, 356.76, 192477.33, 4568.82, 5231.81]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 40.63 ns | 0.268 ns | 0.224 ns | 304 B |
| Imposter | 253.42 ns | 0.773 ns | 0.685 ns | 2400 B |
| Mockolate | 197.21 ns | 1.516 ns | 1.418 ns | 1240 B |
| Moq | 49,919.91 ns | 413.995 ns | 366.996 ns | 6925 B |
| NSubstitute | 2,649.79 ns | 24.549 ns | 21.762 ns | 7088 B |
| FakeItEasy | 2,592.75 ns | 37.551 ns | 35.126 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 59904
  bar [40.63, 253.42, 197.21, 49919.91, 2649.79, 2592.75]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 919.51 ns | 6.907 ns | 5.768 ns | 4176 B |
| Imposter | 1,349.66 ns | 13.457 ns | 11.238 ns | 11192 B |
| Mockolate | 904.30 ns | 7.532 ns | 6.289 ns | 5408 B |
| Moq | 274,210.27 ns | 1,980.387 ns | 1,755.562 ns | 34699 B |
| NSubstitute | 8,213.23 ns | 90.755 ns | 84.893 ns | 16762 B |
| FakeItEasy | 9,272.96 ns | 180.326 ns | 269.903 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 329053
  bar [919.51, 1349.66, 904.3, 274210.27, 8213.23, 9272.96]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-11T03:29:06.162Z*
