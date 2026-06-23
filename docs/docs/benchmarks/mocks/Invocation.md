---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 270.38 ns | 54.11 ns | 2.966 ns | 128 B |
| Imposter | 301.92 ns | 67.50 ns | 3.700 ns | 168 B |
| Mockolate | 126.10 ns | 58.73 ns | 3.219 ns | 84 B |
| Moq | 851.19 ns | 47.78 ns | 2.619 ns | 376 B |
| NSubstitute | 761.34 ns | 377.91 ns | 20.715 ns | 304 B |
| FakeItEasy | 1,846.38 ns | 454.80 ns | 24.929 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2216
  bar [270.38, 301.92, 126.1, 851.19, 761.34, 1846.38]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.75 ns | 70.39 ns | 3.858 ns | 96 B |
| Imposter | 302.30 ns | 92.70 ns | 5.081 ns | 168 B |
| Mockolate | 93.96 ns | 49.49 ns | 2.713 ns | 60 B |
| Moq | 518.41 ns | 69.94 ns | 3.833 ns | 296 B |
| NSubstitute | 615.50 ns | 782.68 ns | 42.901 ns | 272 B |
| FakeItEasy | 1,624.58 ns | 932.90 ns | 51.135 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1950
  bar [164.75, 302.3, 93.96, 518.41, 615.5, 1624.58]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,784.37 ns | 7,980.11 ns | 437.417 ns | 12736 B |
| Imposter | 28,612.65 ns | 8,152.17 ns | 446.848 ns | 16800 B |
| Mockolate | 10,231.96 ns | 2,867.48 ns | 157.176 ns | 8400 B |
| Moq | 81,199.04 ns | 13,310.93 ns | 729.617 ns | 37600 B |
| NSubstitute | 70,203.07 ns | 23,486.86 ns | 1,287.394 ns | 30848 B |
| FakeItEasy | 175,726.07 ns | 39,926.34 ns | 2,188.497 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 210872
  bar [26784.37, 28612.65, 10231.96, 81199.04, 70203.07, 175726.07]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-23T03:26:30.646Z*
