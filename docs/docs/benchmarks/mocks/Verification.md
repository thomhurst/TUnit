---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 707.45 ns | 4.718 ns | 3.940 ns | 3080 B |
| Imposter | 675.42 ns | 4.883 ns | 4.567 ns | 4688 B |
| Mockolate | 915.68 ns | 3.910 ns | 3.657 ns | 3152 B |
| Moq | 338,668.16 ns | 2,770.452 ns | 2,455.934 ns | 24325 B |
| NSubstitute | 6,278.37 ns | 41.745 ns | 37.006 ns | 10064 B |
| FakeItEasy | 7,017.05 ns | 43.449 ns | 38.517 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 406402
  bar [707.45, 675.42, 915.68, 338668.16, 6278.37, 7017.05]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.70 ns | 1.066 ns | 1.094 ns | 328 B |
| Imposter | 340.83 ns | 5.067 ns | 4.739 ns | 2400 B |
| Mockolate | 217.67 ns | 1.432 ns | 1.196 ns | 952 B |
| Moq | 87,289.64 ns | 569.896 ns | 533.081 ns | 6918 B |
| NSubstitute | 3,600.73 ns | 32.406 ns | 30.312 ns | 7088 B |
| FakeItEasy | 3,845.23 ns | 76.468 ns | 81.820 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104748
  bar [53.7, 340.83, 217.67, 87289.64, 3600.73, 3845.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,277.83 ns | 7.505 ns | 7.020 ns | 4608 B |
| Imposter | 1,678.68 ns | 29.496 ns | 26.148 ns | 11192 B |
| Mockolate | 1,852.45 ns | 33.616 ns | 31.444 ns | 5496 B |
| Moq | 462,498.45 ns | 3,366.542 ns | 3,149.066 ns | 34699 B |
| NSubstitute | 11,280.27 ns | 116.674 ns | 103.429 ns | 16929 B |
| FakeItEasy | 13,635.65 ns | 226.040 ns | 200.379 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 554999
  bar [1277.83, 1678.68, 1852.45, 462498.45, 11280.27, 13635.65]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-22T03:22:46.937Z*
