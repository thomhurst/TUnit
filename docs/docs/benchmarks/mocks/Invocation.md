---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 270.4 ns | 92.47 ns | 5.07 ns | 120 B |
| Imposter | 308.5 ns | 92.21 ns | 5.05 ns | 168 B |
| Mockolate | 937.8 ns | 346.32 ns | 18.98 ns | 640 B |
| Moq | 863.8 ns | 394.29 ns | 21.61 ns | 376 B |
| NSubstitute | 746.5 ns | 139.14 ns | 7.63 ns | 304 B |
| FakeItEasy | 1,917.0 ns | 44.18 ns | 2.42 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2301
  bar [270.4, 308.5, 937.8, 863.8, 746.5, 1917]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 163.9 ns | 110.25 ns | 6.04 ns | 88 B |
| Imposter | 309.7 ns | 99.20 ns | 5.44 ns | 168 B |
| Mockolate | 648.5 ns | 546.33 ns | 29.95 ns | 520 B |
| Moq | 588.2 ns | 188.99 ns | 10.36 ns | 296 B |
| NSubstitute | 664.4 ns | 245.65 ns | 13.46 ns | 272 B |
| FakeItEasy | 1,698.6 ns | 323.03 ns | 17.71 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2039
  bar [163.9, 309.7, 648.5, 588.2, 664.4, 1698.6]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,086.2 ns | 16,601.94 ns | 910.01 ns | 11936 B |
| Imposter | 30,284.6 ns | 7,895.03 ns | 432.75 ns | 16800 B |
| Mockolate | 81,795.1 ns | 119,964.61 ns | 6,575.66 ns | 64000 B |
| Moq | 88,144.6 ns | 16,720.64 ns | 916.51 ns | 37600 B |
| NSubstitute | 74,097.0 ns | 16,067.85 ns | 880.73 ns | 30848 B |
| FakeItEasy | 198,368.9 ns | 53,259.89 ns | 2,919.35 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 238043
  bar [27086.2, 30284.6, 81795.1, 88144.6, 74097, 198368.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-19T03:31:38.770Z*
