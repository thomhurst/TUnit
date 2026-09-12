---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 267.75 ns | 72.46 ns | 3.972 ns | 128 B |
| Imposter | 299.18 ns | 85.79 ns | 4.702 ns | 168 B |
| Mockolate | 106.21 ns | 29.16 ns | 1.599 ns | 84 B |
| Moq | 767.87 ns | 156.70 ns | 8.589 ns | 376 B |
| NSubstitute | 704.85 ns | 209.21 ns | 11.468 ns | 304 B |
| FakeItEasy | 1,716.72 ns | 131.04 ns | 7.183 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2061
  bar [267.75, 299.18, 106.21, 767.87, 704.85, 1716.72]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 173.15 ns | 59.48 ns | 3.260 ns | 96 B |
| Imposter | 300.49 ns | 67.16 ns | 3.681 ns | 168 B |
| Mockolate | 95.41 ns | 13.64 ns | 0.747 ns | 60 B |
| Moq | 533.48 ns | 11.40 ns | 0.625 ns | 296 B |
| NSubstitute | 642.95 ns | 148.14 ns | 8.120 ns | 328 B |
| FakeItEasy | 1,560.80 ns | 55.28 ns | 3.030 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1873
  bar [173.15, 300.49, 95.41, 533.48, 642.95, 1560.8]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,588.40 ns | 8,515.12 ns | 466.743 ns | 12736 B |
| Imposter | 29,579.39 ns | 8,453.44 ns | 463.362 ns | 16800 B |
| Mockolate | 10,429.54 ns | 1,658.20 ns | 90.892 ns | 8400 B |
| Moq | 76,463.67 ns | 9,053.34 ns | 496.244 ns | 37600 B |
| NSubstitute | 70,565.07 ns | 15,985.06 ns | 876.195 ns | 30848 B |
| FakeItEasy | 173,043.46 ns | 11,144.79 ns | 610.884 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 207653
  bar [26588.4, 29579.39, 10429.54, 76463.67, 70565.07, 173043.46]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-12T02:33:29.738Z*
