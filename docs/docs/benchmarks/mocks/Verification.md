---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 696.77 ns | 10.856 ns | 10.155 ns | 2864 B |
| Imposter | 818.60 ns | 10.686 ns | 9.996 ns | 4688 B |
| Mockolate | 934.93 ns | 10.463 ns | 9.787 ns | 3152 B |
| Moq | 240,532.48 ns | 945.840 ns | 884.739 ns | 24324 B |
| NSubstitute | 5,983.82 ns | 43.581 ns | 40.766 ns | 10064 B |
| FakeItEasy | 6,611.56 ns | 65.982 ns | 58.492 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 288639
  bar [696.77, 818.6, 934.93, 240532.48, 5983.82, 6611.56]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.32 ns | 0.221 ns | 0.196 ns | 304 B |
| Imposter | 336.44 ns | 4.456 ns | 4.168 ns | 2400 B |
| Mockolate | 239.15 ns | 1.486 ns | 1.390 ns | 952 B |
| Moq | 60,052.67 ns | 180.766 ns | 160.245 ns | 6925 B |
| NSubstitute | 3,500.96 ns | 51.695 ns | 48.355 ns | 7088 B |
| FakeItEasy | 3,363.45 ns | 61.453 ns | 57.483 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 72064
  bar [54.32, 336.44, 239.15, 60052.67, 3500.96, 3363.45]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,171.04 ns | 7.192 ns | 6.728 ns | 4176 B |
| Imposter | 1,706.30 ns | 18.598 ns | 16.487 ns | 11192 B |
| Mockolate | 1,847.00 ns | 25.207 ns | 23.579 ns | 5496 B |
| Moq | 351,236.24 ns | 4,347.858 ns | 3,854.263 ns | 34699 B |
| NSubstitute | 10,623.03 ns | 88.506 ns | 78.458 ns | 16762 B |
| FakeItEasy | 11,833.85 ns | 96.852 ns | 90.596 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 421484
  bar [1171.04, 1706.3, 1847, 351236.24, 10623.03, 11833.85]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-24T03:24:24.137Z*
