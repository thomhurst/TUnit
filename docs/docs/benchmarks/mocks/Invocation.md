---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 264.60 ns | 62.12 ns | 3.405 ns | 120 B |
| Imposter | 302.39 ns | 128.94 ns | 7.068 ns | 168 B |
| Mockolate | 106.91 ns | 31.71 ns | 1.738 ns | 84 B |
| Moq | 787.59 ns | 95.99 ns | 5.262 ns | 376 B |
| NSubstitute | 714.86 ns | 313.36 ns | 17.176 ns | 304 B |
| FakeItEasy | 1,779.38 ns | 171.21 ns | 9.385 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2136
  bar [264.6, 302.39, 106.91, 787.59, 714.86, 1779.38]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.12 ns | 58.97 ns | 3.232 ns | 88 B |
| Imposter | 299.08 ns | 64.40 ns | 3.530 ns | 168 B |
| Mockolate | 99.58 ns | 23.72 ns | 1.300 ns | 60 B |
| Moq | 539.76 ns | 209.98 ns | 11.510 ns | 296 B |
| NSubstitute | 616.67 ns | 160.41 ns | 8.793 ns | 272 B |
| FakeItEasy | 1,587.78 ns | 589.96 ns | 32.338 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1906
  bar [166.12, 299.08, 99.58, 539.76, 616.67, 1587.78]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,403.83 ns | 12,255.55 ns | 671.768 ns | 11936 B |
| Imposter | 29,480.58 ns | 8,518.10 ns | 466.906 ns | 16800 B |
| Mockolate | 10,810.59 ns | 4,843.38 ns | 265.482 ns | 8400 B |
| Moq | 76,844.63 ns | 6,022.98 ns | 330.140 ns | 37600 B |
| NSubstitute | 70,881.18 ns | 33,128.57 ns | 1,815.889 ns | 30848 B |
| FakeItEasy | 174,040.90 ns | 62,358.93 ns | 3,418.103 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208850
  bar [27403.83, 29480.58, 10810.59, 76844.63, 70881.18, 174040.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-24T03:32:03.972Z*
