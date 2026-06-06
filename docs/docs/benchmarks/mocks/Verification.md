---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 772.81 ns | 8.086 ns | 7.168 ns | 3000 B |
| Imposter | 721.31 ns | 9.869 ns | 9.232 ns | 4688 B |
| Mockolate | 411.17 ns | 2.232 ns | 1.863 ns | 2240 B |
| Moq | 246,478.85 ns | 3,024.915 ns | 2,681.508 ns | 24324 B |
| NSubstitute | 5,965.09 ns | 107.286 ns | 100.355 ns | 10064 B |
| FakeItEasy | 6,531.29 ns | 119.980 ns | 117.836 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 295775
  bar [772.81, 721.31, 411.17, 246478.85, 5965.09, 6531.29]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.24 ns | 0.292 ns | 0.273 ns | 312 B |
| Imposter | 334.74 ns | 4.416 ns | 4.131 ns | 2400 B |
| Mockolate | 249.12 ns | 2.031 ns | 1.900 ns | 1240 B |
| Moq | 62,535.70 ns | 515.668 ns | 430.606 ns | 6925 B |
| NSubstitute | 3,415.97 ns | 50.642 ns | 47.371 ns | 7088 B |
| FakeItEasy | 3,264.59 ns | 41.410 ns | 38.735 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75043
  bar [54.24, 334.74, 249.12, 62535.7, 3415.97, 3264.59]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,282.45 ns | 16.715 ns | 14.817 ns | 4464 B |
| Imposter | 1,747.22 ns | 34.408 ns | 62.917 ns | 11192 B |
| Mockolate | 1,142.36 ns | 22.389 ns | 27.496 ns | 5376 B |
| Moq | 357,392.96 ns | 5,745.846 ns | 5,093.543 ns | 34811 B |
| NSubstitute | 10,530.22 ns | 114.442 ns | 107.049 ns | 16762 B |
| FakeItEasy | 11,804.90 ns | 185.273 ns | 173.305 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 428872
  bar [1282.45, 1747.22, 1142.36, 357392.96, 10530.22, 11804.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-06T03:26:59.455Z*
