---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 665.87 ns | 11.608 ns | 10.858 ns | 2864 B |
| Imposter | 688.48 ns | 5.914 ns | 5.242 ns | 4688 B |
| Mockolate | 681.70 ns | 5.054 ns | 4.727 ns | 2880 B |
| Moq | 342,452.78 ns | 1,802.187 ns | 1,685.767 ns | 24325 B |
| NSubstitute | 6,582.40 ns | 84.837 ns | 75.205 ns | 10176 B |
| FakeItEasy | 7,375.59 ns | 62.936 ns | 55.791 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 410944
  bar [665.87, 688.48, 681.7, 342452.78, 6582.4, 7375.59]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.76 ns | 0.958 ns | 0.896 ns | 304 B |
| Imposter | 318.00 ns | 3.776 ns | 3.153 ns | 2400 B |
| Mockolate | 299.38 ns | 2.027 ns | 1.797 ns | 1656 B |
| Moq | 87,628.08 ns | 339.364 ns | 283.384 ns | 6918 B |
| NSubstitute | 3,635.33 ns | 57.538 ns | 53.821 ns | 7088 B |
| FakeItEasy | 3,674.48 ns | 40.840 ns | 34.103 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 105154
  bar [49.76, 318, 299.38, 87628.08, 3635.33, 3674.48]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,154.72 ns | 14.825 ns | 13.868 ns | 4176 B |
| Imposter | 1,677.59 ns | 25.882 ns | 42.524 ns | 11192 B |
| Mockolate | 1,338.39 ns | 6.212 ns | 5.811 ns | 6096 B |
| Moq | 474,917.87 ns | 2,493.215 ns | 2,332.155 ns | 34699 B |
| NSubstitute | 10,999.23 ns | 80.382 ns | 75.189 ns | 16762 B |
| FakeItEasy | 13,790.27 ns | 143.841 ns | 127.511 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569902
  bar [1154.72, 1677.59, 1338.39, 474917.87, 10999.23, 13790.27]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-02T03:24:38.193Z*
