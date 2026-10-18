---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 409.68 ns | 7.361 ns | 6.886 ns | 3040 B |
| Imposter | 330.85 ns | 3.977 ns | 3.321 ns | 4504 B |
| Mockolate | 231.52 ns | 2.330 ns | 1.946 ns | 2128 B |
| Moq | 101,001.39 ns | 1,201.897 ns | 1,065.450 ns | 24340 B |
| NSubstitute | 3,313.63 ns | 34.891 ns | 30.930 ns | 10176 B |
| FakeItEasy | 3,352.41 ns | 12.587 ns | 10.510 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 121202
  bar [409.68, 330.85, 231.52, 101001.39, 3313.63, 3352.41]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 31.06 ns | 0.228 ns | 0.202 ns | 336 B |
| Imposter | 168.36 ns | 2.902 ns | 2.714 ns | 2400 B |
| Mockolate | 130.41 ns | 1.085 ns | 0.906 ns | 1144 B |
| Moq | 24,997.15 ns | 242.045 ns | 214.567 ns | 6925 B |
| NSubstitute | 1,831.54 ns | 6.043 ns | 5.357 ns | 7088 B |
| FakeItEasy | 1,642.53 ns | 9.329 ns | 7.790 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 29997
  bar [31.06, 168.36, 130.41, 24997.15, 1831.54, 1642.53]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 707.71 ns | 10.582 ns | 9.899 ns | 4528 B |
| Imposter | 800.27 ns | 4.305 ns | 3.816 ns | 10840 B |
| Mockolate | 593.95 ns | 3.829 ns | 3.197 ns | 5240 B |
| Moq | 132,379.80 ns | 1,163.701 ns | 1,031.591 ns | 34810 B |
| NSubstitute | 6,026.27 ns | 44.750 ns | 39.670 ns | 16761 B |
| FakeItEasy | 6,032.00 ns | 29.746 ns | 24.839 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 158856
  bar [707.71, 800.27, 593.95, 132379.8, 6026.27, 6032]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-16T03:11:30.127Z*
