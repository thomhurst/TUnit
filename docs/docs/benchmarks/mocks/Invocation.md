---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 283.1 ns | 89.17 ns | 4.89 ns | 128 B |
| Imposter | 306.5 ns | 74.36 ns | 4.08 ns | 168 B |
| Mockolate | 123.4 ns | 22.48 ns | 1.23 ns | 84 B |
| Moq | 863.1 ns | 281.12 ns | 15.41 ns | 376 B |
| NSubstitute | 772.1 ns | 166.22 ns | 9.11 ns | 304 B |
| FakeItEasy | 1,929.6 ns | 508.64 ns | 27.88 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2316
  bar [283.1, 306.5, 123.4, 863.1, 772.1, 1929.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 168.6 ns | 54.28 ns | 2.98 ns | 96 B |
| Imposter | 308.6 ns | 84.66 ns | 4.64 ns | 168 B |
| Mockolate | 111.5 ns | 64.02 ns | 3.51 ns | 60 B |
| Moq | 576.7 ns | 187.04 ns | 10.25 ns | 296 B |
| NSubstitute | 684.3 ns | 197.88 ns | 10.85 ns | 272 B |
| FakeItEasy | 1,586.4 ns | 380.07 ns | 20.83 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1904
  bar [168.6, 308.6, 111.5, 576.7, 684.3, 1586.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,360.0 ns | 15,169.20 ns | 831.47 ns | 12736 B |
| Imposter | 30,120.1 ns | 11,255.75 ns | 616.97 ns | 16800 B |
| Mockolate | 11,402.9 ns | 6,564.79 ns | 359.84 ns | 8400 B |
| Moq | 86,067.1 ns | 45,572.78 ns | 2,498.00 ns | 37600 B |
| NSubstitute | 75,180.6 ns | 76,511.21 ns | 4,193.84 ns | 30848 B |
| FakeItEasy | 186,544.5 ns | 60,044.80 ns | 3,291.26 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 223854
  bar [27360, 30120.1, 11402.9, 86067.1, 75180.6, 186544.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-15T02:33:23.208Z*
