---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 679.79 ns | 7.110 ns | 6.651 ns | 2864 B |
| Imposter | 746.00 ns | 14.515 ns | 19.868 ns | 4688 B |
| Mockolate | 437.10 ns | 8.459 ns | 7.913 ns | 2224 B |
| Moq | 347,606.59 ns | 1,545.823 ns | 1,370.331 ns | 24325 B |
| NSubstitute | 6,402.10 ns | 54.812 ns | 45.771 ns | 10064 B |
| FakeItEasy | 7,738.71 ns | 73.937 ns | 69.161 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417128
  bar [679.79, 746, 437.1, 347606.59, 6402.1, 7738.71]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.97 ns | 0.618 ns | 0.578 ns | 304 B |
| Imposter | 342.97 ns | 4.369 ns | 4.086 ns | 2400 B |
| Mockolate | 255.39 ns | 3.024 ns | 2.681 ns | 1240 B |
| Moq | 88,397.96 ns | 236.942 ns | 197.857 ns | 6918 B |
| NSubstitute | 3,691.93 ns | 24.021 ns | 22.469 ns | 7088 B |
| FakeItEasy | 3,775.20 ns | 35.474 ns | 33.182 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106078
  bar [51.97, 342.97, 255.39, 88397.96, 3691.93, 3775.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,196.78 ns | 7.266 ns | 6.441 ns | 4176 B |
| Imposter | 1,852.12 ns | 18.009 ns | 16.846 ns | 11192 B |
| Mockolate | 1,221.25 ns | 9.778 ns | 8.668 ns | 5408 B |
| Moq | 476,948.23 ns | 2,912.642 ns | 2,724.487 ns | 34699 B |
| NSubstitute | 11,651.23 ns | 43.746 ns | 36.530 ns | 16891 B |
| FakeItEasy | 13,949.31 ns | 236.749 ns | 197.697 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 572338
  bar [1196.78, 1852.12, 1221.25, 476948.23, 11651.23, 13949.31]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-09T03:26:33.451Z*
