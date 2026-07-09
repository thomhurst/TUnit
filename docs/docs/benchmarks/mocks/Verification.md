---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 748.37 ns | 6.193 ns | 5.793 ns | 3008 B |
| Imposter | 673.64 ns | 5.476 ns | 4.855 ns | 4688 B |
| Mockolate | 398.33 ns | 1.379 ns | 1.223 ns | 2128 B |
| Moq | 246,923.12 ns | 1,480.043 ns | 1,235.903 ns | 24324 B |
| NSubstitute | 5,807.42 ns | 26.731 ns | 22.321 ns | 10064 B |
| FakeItEasy | 6,436.14 ns | 31.387 ns | 27.823 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 296308
  bar [748.37, 673.64, 398.33, 246923.12, 5807.42, 6436.14]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.00 ns | 0.164 ns | 0.154 ns | 320 B |
| Imposter | 330.03 ns | 1.168 ns | 1.036 ns | 2400 B |
| Mockolate | 242.30 ns | 1.555 ns | 1.299 ns | 1144 B |
| Moq | 63,460.69 ns | 325.015 ns | 304.019 ns | 6925 B |
| NSubstitute | 3,353.24 ns | 9.178 ns | 8.136 ns | 7088 B |
| FakeItEasy | 3,282.62 ns | 9.810 ns | 7.659 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 76153
  bar [56, 330.03, 242.3, 63460.69, 3353.24, 3282.62]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,251.18 ns | 7.656 ns | 6.787 ns | 4472 B |
| Imposter | 1,753.09 ns | 32.246 ns | 30.163 ns | 11192 B |
| Mockolate | 1,146.45 ns | 15.216 ns | 14.233 ns | 5240 B |
| Moq | 356,541.20 ns | 4,285.811 ns | 3,799.260 ns | 35066 B |
| NSubstitute | 10,374.61 ns | 44.386 ns | 41.519 ns | 16762 B |
| FakeItEasy | 11,568.61 ns | 95.740 ns | 89.556 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 427850
  bar [1251.18, 1753.09, 1146.45, 356541.2, 10374.61, 11568.61]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-09T03:24:10.827Z*
