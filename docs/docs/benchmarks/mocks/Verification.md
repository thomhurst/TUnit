---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 741.98 ns | 1.913 ns | 1.790 ns | 3008 B |
| Imposter | 656.17 ns | 2.498 ns | 2.215 ns | 4688 B |
| Mockolate | 403.81 ns | 2.112 ns | 1.975 ns | 2240 B |
| Moq | 250,183.48 ns | 994.648 ns | 830.576 ns | 24578 B |
| NSubstitute | 5,689.24 ns | 22.429 ns | 19.882 ns | 10064 B |
| FakeItEasy | 6,496.46 ns | 28.324 ns | 25.109 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 300221
  bar [741.98, 656.17, 403.81, 250183.48, 5689.24, 6496.46]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.76 ns | 0.112 ns | 0.094 ns | 320 B |
| Imposter | 326.54 ns | 2.374 ns | 2.105 ns | 2400 B |
| Mockolate | 249.62 ns | 0.819 ns | 0.684 ns | 1240 B |
| Moq | 62,584.21 ns | 618.520 ns | 578.564 ns | 6925 B |
| NSubstitute | 3,376.92 ns | 10.652 ns | 9.964 ns | 7088 B |
| FakeItEasy | 3,209.95 ns | 18.811 ns | 17.595 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75102
  bar [56.76, 326.54, 249.62, 62584.21, 3376.92, 3209.95]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,262.20 ns | 4.986 ns | 4.664 ns | 4472 B |
| Imposter | 1,644.45 ns | 4.998 ns | 4.430 ns | 11192 B |
| Mockolate | 1,114.69 ns | 5.924 ns | 5.252 ns | 5376 B |
| Moq | 343,800.49 ns | 2,577.544 ns | 2,284.925 ns | 34699 B |
| NSubstitute | 10,191.09 ns | 29.891 ns | 24.960 ns | 16762 B |
| FakeItEasy | 11,948.41 ns | 113.923 ns | 95.131 ns | 19456 B |

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
  y-axis "Time (ns)" 0 --> 412561
  bar [1262.2, 1644.45, 1114.69, 343800.49, 10191.09, 11948.41]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-19T03:29:43.427Z*
