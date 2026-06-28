---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 751.20 ns | 10.720 ns | 10.028 ns | 3008 B |
| Imposter | 702.81 ns | 2.961 ns | 2.625 ns | 4688 B |
| Mockolate | 411.36 ns | 3.533 ns | 3.305 ns | 2128 B |
| Moq | 345,726.33 ns | 1,904.655 ns | 1,688.427 ns | 24325 B |
| NSubstitute | 6,276.12 ns | 21.456 ns | 20.070 ns | 10064 B |
| FakeItEasy | 7,661.40 ns | 85.386 ns | 79.871 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 414872
  bar [751.2, 702.81, 411.36, 345726.33, 6276.12, 7661.4]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.55 ns | 1.093 ns | 0.969 ns | 320 B |
| Imposter | 332.38 ns | 2.582 ns | 2.289 ns | 2400 B |
| Mockolate | 231.82 ns | 1.735 ns | 1.538 ns | 1144 B |
| Moq | 88,161.09 ns | 290.816 ns | 242.844 ns | 6918 B |
| NSubstitute | 3,713.99 ns | 13.567 ns | 12.691 ns | 7088 B |
| FakeItEasy | 3,722.62 ns | 68.464 ns | 64.041 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 105794
  bar [54.55, 332.38, 231.82, 88161.09, 3713.99, 3722.62]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,318.97 ns | 26.057 ns | 33.882 ns | 4472 B |
| Imposter | 1,774.48 ns | 17.410 ns | 16.285 ns | 11192 B |
| Mockolate | 1,150.95 ns | 9.130 ns | 7.624 ns | 5240 B |
| Moq | 474,664.82 ns | 1,335.211 ns | 1,183.630 ns | 34699 B |
| NSubstitute | 11,454.25 ns | 45.297 ns | 40.155 ns | 16762 B |
| FakeItEasy | 13,578.86 ns | 165.243 ns | 154.568 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569598
  bar [1318.97, 1774.48, 1150.95, 474664.82, 11454.25, 13578.86]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-28T03:33:50.965Z*
