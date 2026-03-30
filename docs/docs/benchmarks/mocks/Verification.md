---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 859.9 ns | 6.07 ns | 5.07 ns | 3952 B |
| Imposter | 695.4 ns | 8.71 ns | 7.72 ns | 4688 B |
| Mockolate | 917.7 ns | 5.25 ns | 4.65 ns | 3104 B |
| Moq | 346,841.6 ns | 2,828.82 ns | 2,646.08 ns | 24325 B |
| NSubstitute | 6,231.1 ns | 33.63 ns | 31.46 ns | 10064 B |
| FakeItEasy | 7,355.6 ns | 146.69 ns | 137.22 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 416210
  bar [859.9, 695.4, 917.7, 346841.6, 6231.1, 7355.6]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 178.4 ns | 1.20 ns | 1.06 ns | 1304 B |
| Imposter | 313.6 ns | 6.29 ns | 6.99 ns | 2400 B |
| Mockolate | 215.0 ns | 2.40 ns | 2.00 ns | 904 B |
| Moq | 89,288.2 ns | 1,714.30 ns | 2,458.60 ns | 6918 B |
| NSubstitute | 3,656.8 ns | 65.25 ns | 61.03 ns | 7088 B |
| FakeItEasy | 3,626.9 ns | 36.89 ns | 34.51 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107146
  bar [178.4, 313.6, 215, 89288.2, 3656.8, 3626.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,590.1 ns | 14.39 ns | 13.46 ns | 5968 B |
| Imposter | 1,730.3 ns | 20.58 ns | 18.24 ns | 11192 B |
| Mockolate | 1,783.2 ns | 12.49 ns | 11.07 ns | 5400 B |
| Moq | 478,608.6 ns | 2,555.98 ns | 2,390.86 ns | 34954 B |
| NSubstitute | 11,385.0 ns | 99.47 ns | 83.06 ns | 16763 B |
| FakeItEasy | 13,336.0 ns | 130.80 ns | 122.35 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 574331
  bar [1590.1, 1730.3, 1783.2, 478608.6, 11385, 13336]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T21:56:59.028Z*
