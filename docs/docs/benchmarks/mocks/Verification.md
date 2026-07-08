---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 709.70 ns | 13.267 ns | 11.761 ns | 3008 B |
| Imposter | 671.96 ns | 12.634 ns | 11.818 ns | 4688 B |
| Mockolate | 419.05 ns | 6.316 ns | 5.599 ns | 2128 B |
| Moq | 348,262.33 ns | 2,609.011 ns | 2,312.821 ns | 24325 B |
| NSubstitute | 6,294.92 ns | 75.955 ns | 71.048 ns | 10176 B |
| FakeItEasy | 7,243.91 ns | 75.551 ns | 63.089 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417915
  bar [709.7, 671.96, 419.05, 348262.33, 6294.92, 7243.91]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.27 ns | 0.543 ns | 0.482 ns | 320 B |
| Imposter | 325.63 ns | 6.473 ns | 10.992 ns | 2400 B |
| Mockolate | 263.19 ns | 4.640 ns | 4.340 ns | 1144 B |
| Moq | 89,933.78 ns | 241.339 ns | 213.940 ns | 6918 B |
| NSubstitute | 3,838.44 ns | 31.791 ns | 28.182 ns | 7088 B |
| FakeItEasy | 3,853.79 ns | 47.220 ns | 41.859 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107921
  bar [52.27, 325.63, 263.19, 89933.78, 3838.44, 3853.79]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,305.49 ns | 18.849 ns | 17.632 ns | 4472 B |
| Imposter | 1,810.96 ns | 36.261 ns | 53.150 ns | 11192 B |
| Mockolate | 1,092.73 ns | 14.343 ns | 13.417 ns | 5240 B |
| Moq | 479,144.98 ns | 3,589.875 ns | 3,182.331 ns | 35161 B |
| NSubstitute | 11,126.42 ns | 125.151 ns | 110.943 ns | 16762 B |
| FakeItEasy | 13,798.89 ns | 132.717 ns | 124.144 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 574974
  bar [1305.49, 1810.96, 1092.73, 479144.98, 11126.42, 13798.89]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-08T03:21:22.090Z*
