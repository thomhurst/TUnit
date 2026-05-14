---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 842.43 ns | 6.110 ns | 5.715 ns | 2864 B |
| Imposter | 806.27 ns | 9.520 ns | 8.905 ns | 4688 B |
| Mockolate | 458.82 ns | 4.053 ns | 3.593 ns | 2240 B |
| Moq | 351,444.97 ns | 2,445.536 ns | 2,287.556 ns | 24325 B |
| NSubstitute | 6,440.60 ns | 46.885 ns | 41.562 ns | 10064 B |
| FakeItEasy | 7,936.44 ns | 44.148 ns | 39.136 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 421734
  bar [842.43, 806.27, 458.82, 351444.97, 6440.6, 7936.44]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.25 ns | 1.044 ns | 0.872 ns | 304 B |
| Imposter | 365.03 ns | 6.404 ns | 5.991 ns | 2400 B |
| Mockolate | 267.30 ns | 1.930 ns | 1.611 ns | 1240 B |
| Moq | 90,567.96 ns | 273.048 ns | 228.008 ns | 6918 B |
| NSubstitute | 3,707.48 ns | 26.248 ns | 23.268 ns | 7088 B |
| FakeItEasy | 3,856.74 ns | 30.568 ns | 27.098 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 108682
  bar [54.25, 365.03, 267.3, 90567.96, 3707.48, 3856.74]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,245.26 ns | 14.700 ns | 13.031 ns | 4176 B |
| Imposter | 1,971.88 ns | 33.600 ns | 31.430 ns | 11192 B |
| Mockolate | 1,269.81 ns | 13.547 ns | 12.672 ns | 5376 B |
| Moq | 479,796.25 ns | 2,378.701 ns | 2,225.039 ns | 34699 B |
| NSubstitute | 11,809.38 ns | 79.606 ns | 66.475 ns | 16763 B |
| FakeItEasy | 14,325.62 ns | 193.806 ns | 161.837 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 575756
  bar [1245.26, 1971.88, 1269.81, 479796.25, 11809.38, 14325.62]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-14T03:27:14.658Z*
