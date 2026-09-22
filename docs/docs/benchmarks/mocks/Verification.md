---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 721.27 ns | 6.663 ns | 6.232 ns | 3008 B |
| Imposter | 682.34 ns | 5.830 ns | 5.453 ns | 4688 B |
| Mockolate | 407.94 ns | 2.813 ns | 2.494 ns | 2128 B |
| Moq | 343,440.00 ns | 2,566.801 ns | 2,275.402 ns | 24325 B |
| NSubstitute | 6,731.68 ns | 18.429 ns | 17.238 ns | 10176 B |
| FakeItEasy | 7,158.32 ns | 80.758 ns | 75.541 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 412128
  bar [721.27, 682.34, 407.94, 343440, 6731.68, 7158.32]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.15 ns | 0.673 ns | 0.562 ns | 320 B |
| Imposter | 311.65 ns | 6.224 ns | 9.691 ns | 2400 B |
| Mockolate | 252.82 ns | 5.018 ns | 9.669 ns | 1144 B |
| Moq | 89,062.89 ns | 467.351 ns | 414.294 ns | 6918 B |
| NSubstitute | 3,825.68 ns | 59.126 ns | 55.306 ns | 7088 B |
| FakeItEasy | 3,473.18 ns | 25.037 ns | 19.548 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106876
  bar [50.15, 311.65, 252.82, 89062.89, 3825.68, 3473.18]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,225.36 ns | 17.087 ns | 15.983 ns | 4472 B |
| Imposter | 1,704.59 ns | 12.204 ns | 10.191 ns | 11192 B |
| Mockolate | 1,145.79 ns | 22.717 ns | 33.298 ns | 5240 B |
| Moq | 494,852.13 ns | 2,576.266 ns | 2,011.379 ns | 34922 B |
| NSubstitute | 12,531.34 ns | 104.326 ns | 97.586 ns | 16762 B |
| FakeItEasy | 13,435.67 ns | 266.426 ns | 249.215 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 593823
  bar [1225.36, 1704.59, 1145.79, 494852.13, 12531.34, 13435.67]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-22T02:33:33.738Z*
