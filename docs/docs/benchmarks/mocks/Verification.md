---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 760.75 ns | 4.092 ns | 3.828 ns | 3008 B |
| Imposter | 680.80 ns | 5.407 ns | 4.793 ns | 4688 B |
| Mockolate | 398.57 ns | 0.992 ns | 0.829 ns | 2128 B |
| Moq | 240,480.10 ns | 1,310.717 ns | 1,161.917 ns | 24324 B |
| NSubstitute | 6,464.94 ns | 50.175 ns | 41.898 ns | 10064 B |
| FakeItEasy | 6,411.13 ns | 29.251 ns | 25.930 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 288577
  bar [760.75, 680.8, 398.57, 240480.1, 6464.94, 6411.13]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 55.52 ns | 0.206 ns | 0.183 ns | 320 B |
| Imposter | 335.09 ns | 0.901 ns | 0.753 ns | 2400 B |
| Mockolate | 243.15 ns | 0.491 ns | 0.435 ns | 1144 B |
| Moq | 61,824.78 ns | 234.600 ns | 195.902 ns | 6925 B |
| NSubstitute | 3,588.04 ns | 13.947 ns | 12.363 ns | 7088 B |
| FakeItEasy | 3,258.96 ns | 49.439 ns | 46.246 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74190
  bar [55.52, 335.09, 243.15, 61824.78, 3588.04, 3258.96]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,261.68 ns | 2.316 ns | 2.167 ns | 4472 B |
| Imposter | 1,660.58 ns | 5.565 ns | 4.933 ns | 11192 B |
| Mockolate | 1,137.61 ns | 3.246 ns | 3.036 ns | 5240 B |
| Moq | 350,973.61 ns | 2,881.199 ns | 2,695.076 ns | 34699 B |
| NSubstitute | 11,253.45 ns | 35.831 ns | 29.920 ns | 16762 B |
| FakeItEasy | 11,742.28 ns | 65.267 ns | 61.051 ns | 19344 B |

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
  y-axis "Time (ns)" 0 --> 421169
  bar [1261.68, 1660.58, 1137.61, 350973.61, 11253.45, 11742.28]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-26T02:57:20.474Z*
