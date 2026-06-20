---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 732.40 ns | 14.582 ns | 14.321 ns | 3008 B |
| Imposter | 844.58 ns | 16.893 ns | 18.076 ns | 4688 B |
| Mockolate | 429.26 ns | 8.504 ns | 15.334 ns | 2240 B |
| Moq | 346,811.53 ns | 1,385.095 ns | 1,156.617 ns | 24325 B |
| NSubstitute | 6,310.12 ns | 46.967 ns | 39.220 ns | 10064 B |
| FakeItEasy | 7,701.86 ns | 82.198 ns | 76.889 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 416174
  bar [732.4, 844.58, 429.26, 346811.53, 6310.12, 7701.86]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.41 ns | 0.821 ns | 0.768 ns | 320 B |
| Imposter | 331.63 ns | 6.563 ns | 10.965 ns | 2400 B |
| Mockolate | 245.30 ns | 4.836 ns | 4.750 ns | 1240 B |
| Moq | 88,358.65 ns | 477.895 ns | 423.642 ns | 6918 B |
| NSubstitute | 3,693.97 ns | 25.801 ns | 24.134 ns | 7088 B |
| FakeItEasy | 3,737.11 ns | 43.199 ns | 40.408 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106031
  bar [53.41, 331.63, 245.3, 88358.65, 3693.97, 3737.11]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,273.56 ns | 21.880 ns | 19.396 ns | 4472 B |
| Imposter | 1,775.45 ns | 29.853 ns | 33.182 ns | 11192 B |
| Mockolate | 1,236.29 ns | 24.738 ns | 24.296 ns | 5376 B |
| Moq | 477,723.02 ns | 2,594.361 ns | 2,299.834 ns | 34842 B |
| NSubstitute | 11,394.50 ns | 105.163 ns | 87.816 ns | 16762 B |
| FakeItEasy | 13,736.76 ns | 156.434 ns | 146.328 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 573268
  bar [1273.56, 1775.45, 1236.29, 477723.02, 11394.5, 13736.76]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-20T03:29:22.484Z*
