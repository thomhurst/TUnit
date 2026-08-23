---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 277.6 ns | 88.49 ns | 4.85 ns | 128 B |
| Imposter | 305.4 ns | 55.88 ns | 3.06 ns | 168 B |
| Mockolate | 125.4 ns | 21.13 ns | 1.16 ns | 84 B |
| Moq | 841.1 ns | 190.78 ns | 10.46 ns | 376 B |
| NSubstitute | 758.6 ns | 162.06 ns | 8.88 ns | 304 B |
| FakeItEasy | 1,864.1 ns | 374.08 ns | 20.50 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2237
  bar [277.6, 305.4, 125.4, 841.1, 758.6, 1864.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.4 ns | 69.56 ns | 3.81 ns | 96 B |
| Imposter | 303.4 ns | 58.71 ns | 3.22 ns | 168 B |
| Mockolate | 110.1 ns | 8.07 ns | 0.44 ns | 60 B |
| Moq | 568.2 ns | 157.12 ns | 8.61 ns | 296 B |
| NSubstitute | 652.4 ns | 202.96 ns | 11.13 ns | 272 B |
| FakeItEasy | 1,681.9 ns | 306.49 ns | 16.80 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2019
  bar [165.4, 303.4, 110.1, 568.2, 652.4, 1681.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,133.1 ns | 12,820.69 ns | 702.75 ns | 12736 B |
| Imposter | 30,478.6 ns | 10,685.94 ns | 585.73 ns | 16800 B |
| Mockolate | 12,661.0 ns | 7,483.08 ns | 410.17 ns | 8400 B |
| Moq | 86,517.0 ns | 13,497.10 ns | 739.82 ns | 37600 B |
| NSubstitute | 74,900.2 ns | 49,573.10 ns | 2,717.27 ns | 30848 B |
| FakeItEasy | 192,880.1 ns | 75,583.34 ns | 4,142.98 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 231457
  bar [28133.1, 30478.6, 12661, 86517, 74900.2, 192880.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-23T02:45:27.613Z*
