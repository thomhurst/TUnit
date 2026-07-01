---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 759.26 ns | 5.934 ns | 5.550 ns | 3008 B |
| Imposter | 759.20 ns | 9.621 ns | 8.999 ns | 4688 B |
| Mockolate | 430.53 ns | 8.073 ns | 7.156 ns | 2128 B |
| Moq | 356,238.88 ns | 4,096.213 ns | 3,631.186 ns | 24644 B |
| NSubstitute | 6,461.83 ns | 60.923 ns | 56.987 ns | 10064 B |
| FakeItEasy | 7,704.45 ns | 51.603 ns | 48.270 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 427487
  bar [759.26, 759.2, 430.53, 356238.88, 6461.83, 7704.45]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.39 ns | 1.133 ns | 1.113 ns | 320 B |
| Imposter | 354.20 ns | 6.343 ns | 5.933 ns | 2400 B |
| Mockolate | 254.05 ns | 1.629 ns | 1.444 ns | 1144 B |
| Moq | 89,699.75 ns | 500.995 ns | 444.119 ns | 6918 B |
| NSubstitute | 3,867.83 ns | 17.410 ns | 14.538 ns | 7088 B |
| FakeItEasy | 3,712.18 ns | 46.103 ns | 40.869 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107640
  bar [56.39, 354.2, 254.05, 89699.75, 3867.83, 3712.18]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,358.55 ns | 14.748 ns | 13.795 ns | 4472 B |
| Imposter | 1,875.53 ns | 36.602 ns | 35.948 ns | 11192 B |
| Mockolate | 1,172.54 ns | 17.212 ns | 15.258 ns | 5240 B |
| Moq | 485,295.42 ns | 3,736.525 ns | 3,495.148 ns | 34699 B |
| NSubstitute | 11,762.84 ns | 62.423 ns | 52.126 ns | 16763 B |
| FakeItEasy | 13,619.35 ns | 147.782 ns | 138.236 ns | 19346 B |

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
  y-axis "Time (ns)" 0 --> 582355
  bar [1358.55, 1875.53, 1172.54, 485295.42, 11762.84, 13619.35]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-01T03:29:08.803Z*
