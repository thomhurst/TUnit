---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 194.53 ns | 180.94 ns | 9.918 ns | 128 B |
| Imposter | 106.20 ns | 17.34 ns | 0.950 ns | 96 B |
| Mockolate | 87.69 ns | 70.57 ns | 3.868 ns | 84 B |
| Moq | 454.11 ns | 230.72 ns | 12.647 ns | 376 B |
| NSubstitute | 448.31 ns | 180.56 ns | 9.897 ns | 304 B |
| FakeItEasy | 1,272.55 ns | 449.30 ns | 24.628 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1528
  bar [194.53, 106.2, 87.69, 454.11, 448.31, 1272.55]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 131.47 ns | 114.15 ns | 6.257 ns | 96 B |
| Imposter | 106.28 ns | 33.29 ns | 1.825 ns | 96 B |
| Mockolate | 75.37 ns | 62.98 ns | 3.452 ns | 60 B |
| Moq | 354.56 ns | 224.64 ns | 12.313 ns | 296 B |
| NSubstitute | 431.30 ns | 133.97 ns | 7.343 ns | 328 B |
| FakeItEasy | 1,176.39 ns | 166.06 ns | 9.103 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1412
  bar [131.47, 106.28, 75.37, 354.56, 431.3, 1176.39]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 19,754.04 ns | 17,482.39 ns | 958.269 ns | 12736 B |
| Imposter | 10,259.62 ns | 4,574.02 ns | 250.718 ns | 9600 B |
| Mockolate | 8,989.01 ns | 2,578.89 ns | 141.358 ns | 8400 B |
| Moq | 49,331.31 ns | 43,408.07 ns | 2,379.343 ns | 37600 B |
| NSubstitute | 46,336.55 ns | 5,032.94 ns | 275.873 ns | 30848 B |
| FakeItEasy | 117,025.79 ns | 61,036.18 ns | 3,345.599 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 140431
  bar [19754.04, 10259.62, 8989.01, 49331.31, 46336.55, 117025.79]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-16T03:11:30.127Z*
