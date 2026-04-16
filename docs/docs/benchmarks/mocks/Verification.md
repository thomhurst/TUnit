---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 709.52 ns | 1.363 ns | 1.138 ns | 3080 B |
| Imposter | 654.41 ns | 4.892 ns | 4.576 ns | 4688 B |
| Mockolate | 922.90 ns | 1.553 ns | 1.377 ns | 3152 B |
| Moq | 335,454.92 ns | 2,661.938 ns | 2,489.978 ns | 24325 B |
| NSubstitute | 6,129.28 ns | 29.884 ns | 27.954 ns | 10064 B |
| FakeItEasy | 7,205.45 ns | 21.911 ns | 19.424 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 402546
  bar [709.52, 654.41, 922.9, 335454.92, 6129.28, 7205.45]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 59.95 ns | 0.451 ns | 0.422 ns | 328 B |
| Imposter | 314.43 ns | 1.048 ns | 0.929 ns | 2400 B |
| Mockolate | 211.67 ns | 0.971 ns | 0.908 ns | 952 B |
| Moq | 85,724.92 ns | 135.791 ns | 120.375 ns | 6918 B |
| NSubstitute | 3,567.38 ns | 12.592 ns | 11.778 ns | 7088 B |
| FakeItEasy | 3,545.33 ns | 15.567 ns | 12.999 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 102870
  bar [59.95, 314.43, 211.67, 85724.92, 3567.38, 3545.33]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,286.07 ns | 13.249 ns | 12.394 ns | 4608 B |
| Imposter | 1,635.13 ns | 5.646 ns | 5.282 ns | 11192 B |
| Mockolate | 1,783.27 ns | 7.720 ns | 6.843 ns | 5496 B |
| Moq | 458,716.34 ns | 1,253.308 ns | 1,046.569 ns | 34699 B |
| NSubstitute | 11,132.14 ns | 69.879 ns | 65.365 ns | 16763 B |
| FakeItEasy | 12,981.76 ns | 101.066 ns | 89.593 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 550460
  bar [1286.07, 1635.13, 1783.27, 458716.34, 11132.14, 12981.76]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-16T03:23:00.282Z*
