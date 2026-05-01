---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 254.39 ns | 64.67 ns | 3.545 ns | 120 B |
| Imposter | 294.29 ns | 91.63 ns | 5.022 ns | 168 B |
| Mockolate | 112.49 ns | 215.94 ns | 11.837 ns | 84 B |
| Moq | 799.50 ns | 322.96 ns | 17.703 ns | 376 B |
| NSubstitute | 695.80 ns | 110.54 ns | 6.059 ns | 304 B |
| FakeItEasy | 1,769.29 ns | 1,020.00 ns | 55.909 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2124
  bar [254.39, 294.29, 112.49, 799.5, 695.8, 1769.29]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 151.13 ns | 68.39 ns | 3.749 ns | 88 B |
| Imposter | 290.57 ns | 85.07 ns | 4.663 ns | 168 B |
| Mockolate | 86.05 ns | 58.11 ns | 3.185 ns | 60 B |
| Moq | 521.73 ns | 106.97 ns | 5.863 ns | 296 B |
| NSubstitute | 622.83 ns | 386.31 ns | 21.175 ns | 272 B |
| FakeItEasy | 1,544.51 ns | 326.81 ns | 17.914 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1854
  bar [151.13, 290.57, 86.05, 521.73, 622.83, 1544.51]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,482.16 ns | 13,787.40 ns | 755.734 ns | 11936 B |
| Imposter | 28,775.04 ns | 3,459.37 ns | 189.620 ns | 16800 B |
| Mockolate | 10,446.39 ns | 3,622.50 ns | 198.561 ns | 8400 B |
| Moq | 81,238.25 ns | 44,854.77 ns | 2,458.641 ns | 37600 B |
| NSubstitute | 68,892.38 ns | 13,743.14 ns | 753.308 ns | 30848 B |
| FakeItEasy | 177,470.37 ns | 59,155.06 ns | 3,242.488 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 212965
  bar [25482.16, 28775.04, 10446.39, 81238.25, 68892.38, 177470.37]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-01T03:25:57.964Z*
