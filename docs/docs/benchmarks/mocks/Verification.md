---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 746.83 ns | 9.988 ns | 8.854 ns | 3080 B |
| Imposter | 716.93 ns | 14.244 ns | 17.493 ns | 4688 B |
| Mockolate | 912.35 ns | 15.745 ns | 14.728 ns | 3104 B |
| Moq | 339,799.40 ns | 2,662.409 ns | 2,360.156 ns | 24325 B |
| NSubstitute | 6,199.09 ns | 42.877 ns | 40.107 ns | 10064 B |
| FakeItEasy | 7,322.17 ns | 92.879 ns | 86.879 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 407760
  bar [746.83, 716.93, 912.35, 339799.4, 6199.09, 7322.17]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 78.52 ns | 1.512 ns | 1.414 ns | 384 B |
| Imposter | 317.21 ns | 5.982 ns | 5.875 ns | 2400 B |
| Mockolate | 214.45 ns | 3.589 ns | 3.357 ns | 904 B |
| Moq | 86,112.66 ns | 303.650 ns | 284.034 ns | 6918 B |
| NSubstitute | 3,593.45 ns | 71.235 ns | 73.153 ns | 7088 B |
| FakeItEasy | 3,737.39 ns | 28.234 ns | 23.577 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103336
  bar [78.52, 317.21, 214.45, 86112.66, 3593.45, 3737.39]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,349.02 ns | 17.220 ns | 16.108 ns | 4544 B |
| Imposter | 1,761.95 ns | 34.072 ns | 43.090 ns | 11192 B |
| Mockolate | 1,762.67 ns | 10.303 ns | 9.133 ns | 5400 B |
| Moq | 466,487.99 ns | 2,822.770 ns | 2,502.312 ns | 34811 B |
| NSubstitute | 11,333.51 ns | 57.622 ns | 53.900 ns | 16762 B |
| FakeItEasy | 13,367.17 ns | 244.484 ns | 228.691 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 559786
  bar [1349.02, 1761.95, 1762.67, 466487.99, 11333.51, 13367.17]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-03T03:23:45.860Z*
