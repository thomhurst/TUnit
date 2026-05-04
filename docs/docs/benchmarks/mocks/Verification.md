---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 650.23 ns | 5.652 ns | 5.287 ns | 2864 B |
| Imposter | 678.46 ns | 3.198 ns | 2.992 ns | 4688 B |
| Mockolate | 390.63 ns | 1.068 ns | 0.999 ns | 2224 B |
| Moq | 336,564.76 ns | 2,843.926 ns | 2,660.210 ns | 24325 B |
| NSubstitute | 6,141.24 ns | 29.519 ns | 24.650 ns | 10176 B |
| FakeItEasy | 7,042.11 ns | 25.420 ns | 22.534 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 403878
  bar [650.23, 678.46, 390.63, 336564.76, 6141.24, 7042.11]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 47.69 ns | 0.285 ns | 0.267 ns | 304 B |
| Imposter | 308.68 ns | 1.465 ns | 1.298 ns | 2400 B |
| Mockolate | 234.34 ns | 1.085 ns | 0.962 ns | 1240 B |
| Moq | 86,606.18 ns | 293.874 ns | 260.512 ns | 6918 B |
| NSubstitute | 3,511.67 ns | 6.119 ns | 4.777 ns | 7088 B |
| FakeItEasy | 3,508.18 ns | 8.200 ns | 6.847 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103928
  bar [47.69, 308.68, 234.34, 86606.18, 3511.67, 3508.18]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,138.72 ns | 3.709 ns | 3.288 ns | 4176 B |
| Imposter | 1,707.14 ns | 7.728 ns | 6.851 ns | 11192 B |
| Mockolate | 1,112.94 ns | 3.114 ns | 2.913 ns | 5408 B |
| Moq | 467,026.33 ns | 3,771.042 ns | 3,527.435 ns | 34699 B |
| NSubstitute | 11,083.74 ns | 47.137 ns | 39.362 ns | 16762 B |
| FakeItEasy | 12,646.79 ns | 70.125 ns | 58.557 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 560432
  bar [1138.72, 1707.14, 1112.94, 467026.33, 11083.74, 12646.79]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-04T03:27:14.154Z*
