---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 255.66 ns | 50.888 ns | 2.789 ns | 120 B |
| Imposter | 292.65 ns | 95.004 ns | 5.208 ns | 168 B |
| Mockolate | 98.90 ns | 9.432 ns | 0.517 ns | 84 B |
| Moq | 792.14 ns | 97.081 ns | 5.321 ns | 376 B |
| NSubstitute | 759.23 ns | 284.626 ns | 15.601 ns | 360 B |
| FakeItEasy | 1,659.69 ns | 148.704 ns | 8.151 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1992
  bar [255.66, 292.65, 98.9, 792.14, 759.23, 1659.69]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 151.62 ns | 67.067 ns | 3.676 ns | 88 B |
| Imposter | 291.29 ns | 65.330 ns | 3.581 ns | 168 B |
| Mockolate | 93.64 ns | 35.119 ns | 1.925 ns | 60 B |
| Moq | 514.88 ns | 83.119 ns | 4.556 ns | 296 B |
| NSubstitute | 624.13 ns | 75.636 ns | 4.146 ns | 328 B |
| FakeItEasy | 1,486.10 ns | 109.268 ns | 5.989 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1784
  bar [151.62, 291.29, 93.64, 514.88, 624.13, 1486.1]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,740.87 ns | 10,477.861 ns | 574.327 ns | 11936 B |
| Imposter | 28,404.96 ns | 3,657.442 ns | 200.477 ns | 16800 B |
| Mockolate | 9,831.15 ns | 1,669.614 ns | 91.517 ns | 8400 B |
| Moq | 77,229.62 ns | 8,028.239 ns | 440.055 ns | 37600 B |
| NSubstitute | 67,921.86 ns | 7,682.708 ns | 421.115 ns | 30848 B |
| FakeItEasy | 173,055.26 ns | 94,892.964 ns | 5,201.404 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 207667
  bar [25740.87, 28404.96, 9831.15, 77229.62, 67921.86, 173055.26]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-18T03:29:10.052Z*
