---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 262.9 ns | 188.59 ns | 10.34 ns | 120 B |
| Imposter | 292.5 ns | 19.76 ns | 1.08 ns | 168 B |
| Mockolate | 650.3 ns | 41.40 ns | 2.27 ns | 640 B |
| Moq | 791.2 ns | 397.45 ns | 21.79 ns | 376 B |
| NSubstitute | 708.1 ns | 152.17 ns | 8.34 ns | 304 B |
| FakeItEasy | 1,721.6 ns | 112.63 ns | 6.17 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2066
  bar [262.9, 292.5, 650.3, 791.2, 708.1, 1721.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 152.9 ns | 77.91 ns | 4.27 ns | 88 B |
| Imposter | 293.4 ns | 89.66 ns | 4.91 ns | 168 B |
| Mockolate | 521.2 ns | 116.62 ns | 6.39 ns | 520 B |
| Moq | 538.2 ns | 87.62 ns | 4.80 ns | 296 B |
| NSubstitute | 646.0 ns | 219.62 ns | 12.04 ns | 272 B |
| FakeItEasy | 1,591.0 ns | 436.93 ns | 23.95 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1910
  bar [152.9, 293.4, 521.2, 538.2, 646, 1591]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,083.9 ns | 8,032.66 ns | 440.30 ns | 11936 B |
| Imposter | 28,720.1 ns | 11,356.45 ns | 622.49 ns | 16800 B |
| Mockolate | 65,145.9 ns | 25,002.37 ns | 1,370.46 ns | 64000 B |
| Moq | 81,919.3 ns | 13,824.23 ns | 757.75 ns | 37600 B |
| NSubstitute | 75,260.7 ns | 6,705.56 ns | 367.55 ns | 36448 B |
| FakeItEasy | 173,889.2 ns | 59,893.38 ns | 3,282.96 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208668
  bar [26083.9, 28720.1, 65145.9, 81919.3, 75260.7, 173889.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-30T03:25:10.403Z*
