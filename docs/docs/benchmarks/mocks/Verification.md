---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 718.14 ns | 8.139 ns | 7.215 ns | 3008 B |
| Imposter | 669.02 ns | 3.802 ns | 3.371 ns | 4688 B |
| Mockolate | 417.16 ns | 1.682 ns | 1.491 ns | 2128 B |
| Moq | 340,982.74 ns | 3,015.259 ns | 2,672.949 ns | 24325 B |
| NSubstitute | 6,297.55 ns | 69.554 ns | 61.658 ns | 10064 B |
| FakeItEasy | 7,689.72 ns | 85.689 ns | 80.154 ns | 10964 B |

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
  y-axis "Time (ns)" 0 --> 409180
  bar [718.14, 669.02, 417.16, 340982.74, 6297.55, 7689.72]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.40 ns | 1.114 ns | 1.921 ns | 320 B |
| Imposter | 321.83 ns | 6.478 ns | 15.769 ns | 2400 B |
| Mockolate | 235.12 ns | 4.736 ns | 9.011 ns | 1144 B |
| Moq | 87,693.57 ns | 393.981 ns | 328.992 ns | 6918 B |
| NSubstitute | 3,696.62 ns | 32.042 ns | 28.404 ns | 7088 B |
| FakeItEasy | 3,466.32 ns | 31.354 ns | 26.182 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 105233
  bar [54.4, 321.83, 235.12, 87693.57, 3696.62, 3466.32]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,187.82 ns | 5.787 ns | 4.518 ns | 4472 B |
| Imposter | 1,675.59 ns | 30.242 ns | 47.967 ns | 11192 B |
| Mockolate | 1,089.86 ns | 21.674 ns | 24.091 ns | 5240 B |
| Moq | 473,954.94 ns | 4,289.089 ns | 3,802.166 ns | 34699 B |
| NSubstitute | 11,385.19 ns | 94.936 ns | 79.276 ns | 16891 B |
| FakeItEasy | 13,535.94 ns | 99.323 ns | 88.047 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 568746
  bar [1187.82, 1675.59, 1089.86, 473954.94, 11385.19, 13535.94]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-30T03:21:07.533Z*
