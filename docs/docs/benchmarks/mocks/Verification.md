---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 728.82 ns | 10.023 ns | 8.885 ns | 3008 B |
| Imposter | 713.43 ns | 10.551 ns | 9.870 ns | 4688 B |
| Mockolate | 407.53 ns | 7.272 ns | 6.802 ns | 2128 B |
| Moq | 352,534.07 ns | 1,520.499 ns | 1,347.883 ns | 24325 B |
| NSubstitute | 7,160.26 ns | 89.175 ns | 83.415 ns | 10064 B |
| FakeItEasy | 7,356.99 ns | 67.992 ns | 63.599 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 423041
  bar [728.82, 713.43, 407.53, 352534.07, 7160.26, 7356.99]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.86 ns | 0.374 ns | 0.331 ns | 320 B |
| Imposter | 324.44 ns | 4.987 ns | 4.665 ns | 2400 B |
| Mockolate | 236.13 ns | 4.547 ns | 4.253 ns | 1144 B |
| Moq | 89,856.67 ns | 266.966 ns | 236.659 ns | 6918 B |
| NSubstitute | 3,922.36 ns | 20.638 ns | 19.304 ns | 7088 B |
| FakeItEasy | 3,727.84 ns | 63.273 ns | 56.090 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107829
  bar [52.86, 324.44, 236.13, 89856.67, 3922.36, 3727.84]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,254.15 ns | 13.958 ns | 12.373 ns | 4472 B |
| Imposter | 1,827.19 ns | 27.376 ns | 25.608 ns | 11192 B |
| Mockolate | 1,141.55 ns | 19.926 ns | 18.639 ns | 5240 B |
| Moq | 490,117.67 ns | 1,968.825 ns | 1,841.640 ns | 34699 B |
| NSubstitute | 12,577.12 ns | 143.006 ns | 126.771 ns | 16763 B |
| FakeItEasy | 13,775.11 ns | 171.169 ns | 142.934 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 588142
  bar [1254.15, 1827.19, 1141.55, 490117.67, 12577.12, 13775.11]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-23T02:34:56.618Z*
