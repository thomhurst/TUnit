---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 754.70 ns | 3.145 ns | 2.626 ns | 3008 B |
| Imposter | 762.63 ns | 10.501 ns | 9.309 ns | 4688 B |
| Mockolate | 441.79 ns | 1.731 ns | 1.619 ns | 2128 B |
| Moq | 355,772.44 ns | 2,693.322 ns | 2,249.046 ns | 24548 B |
| NSubstitute | 6,724.41 ns | 37.582 ns | 35.155 ns | 10064 B |
| FakeItEasy | 7,730.69 ns | 36.067 ns | 33.737 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 426927
  bar [754.7, 762.63, 441.79, 355772.44, 6724.41, 7730.69]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.72 ns | 0.811 ns | 0.758 ns | 320 B |
| Imposter | 374.86 ns | 7.316 ns | 8.132 ns | 2400 B |
| Mockolate | 256.21 ns | 3.189 ns | 2.827 ns | 1144 B |
| Moq | 91,295.09 ns | 615.636 ns | 545.745 ns | 6918 B |
| NSubstitute | 3,910.38 ns | 12.520 ns | 11.099 ns | 7088 B |
| FakeItEasy | 3,795.80 ns | 39.284 ns | 34.825 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 109555
  bar [57.72, 374.86, 256.21, 91295.09, 3910.38, 3795.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,343.92 ns | 10.260 ns | 9.597 ns | 4472 B |
| Imposter | 2,020.39 ns | 24.216 ns | 22.652 ns | 11192 B |
| Mockolate | 1,237.95 ns | 17.000 ns | 15.902 ns | 5240 B |
| Moq | 487,172.37 ns | 3,579.170 ns | 3,347.958 ns | 34699 B |
| NSubstitute | 12,124.97 ns | 82.940 ns | 73.524 ns | 16763 B |
| FakeItEasy | 13,977.86 ns | 135.823 ns | 120.404 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 584607
  bar [1343.92, 2020.39, 1237.95, 487172.37, 12124.97, 13977.86]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-02T03:23:38.806Z*
