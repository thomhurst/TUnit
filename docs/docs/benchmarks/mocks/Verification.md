---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 850.29 ns | 2.391 ns | 1.997 ns | 2864 B |
| Imposter | 687.26 ns | 6.695 ns | 6.262 ns | 4688 B |
| Mockolate | 549.83 ns | 0.915 ns | 0.856 ns | 2880 B |
| Moq | 343,780.89 ns | 2,001.266 ns | 1,871.986 ns | 24349 B |
| NSubstitute | 6,393.64 ns | 24.927 ns | 19.462 ns | 10064 B |
| FakeItEasy | 7,370.71 ns | 91.827 ns | 85.895 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 412538
  bar [850.29, 687.26, 549.83, 343780.89, 6393.64, 7370.71]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 48.04 ns | 0.173 ns | 0.144 ns | 304 B |
| Imposter | 306.63 ns | 2.252 ns | 2.106 ns | 2400 B |
| Mockolate | 301.35 ns | 1.880 ns | 1.758 ns | 1656 B |
| Moq | 88,090.19 ns | 492.445 ns | 436.540 ns | 6918 B |
| NSubstitute | 3,665.58 ns | 13.075 ns | 10.918 ns | 7088 B |
| FakeItEasy | 3,615.10 ns | 14.044 ns | 13.137 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 105709
  bar [48.04, 306.63, 301.35, 88090.19, 3665.58, 3615.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,105.25 ns | 3.038 ns | 2.537 ns | 4176 B |
| Imposter | 1,695.66 ns | 18.864 ns | 17.646 ns | 11192 B |
| Mockolate | 1,310.79 ns | 13.036 ns | 12.193 ns | 6096 B |
| Moq | 467,376.00 ns | 2,996.182 ns | 2,501.948 ns | 34811 B |
| NSubstitute | 11,581.89 ns | 45.732 ns | 35.705 ns | 16763 B |
| FakeItEasy | 13,755.33 ns | 112.269 ns | 93.749 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 560852
  bar [1105.25, 1695.66, 1310.79, 467376, 11581.89, 13755.33]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-01T03:25:57.964Z*
