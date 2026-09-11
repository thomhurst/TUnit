---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 280.6 ns | 80.91 ns | 4.43 ns | 128 B |
| Imposter | 313.3 ns | 76.33 ns | 4.18 ns | 168 B |
| Mockolate | 136.8 ns | 126.46 ns | 6.93 ns | 84 B |
| Moq | 895.5 ns | 228.08 ns | 12.50 ns | 376 B |
| NSubstitute | 787.3 ns | 434.76 ns | 23.83 ns | 304 B |
| FakeItEasy | 1,901.1 ns | 438.53 ns | 24.04 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2282
  bar [280.6, 313.3, 136.8, 895.5, 787.3, 1901.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 177.2 ns | 66.04 ns | 3.62 ns | 96 B |
| Imposter | 309.1 ns | 115.77 ns | 6.35 ns | 168 B |
| Mockolate | 110.2 ns | 30.78 ns | 1.69 ns | 60 B |
| Moq | 581.4 ns | 160.69 ns | 8.81 ns | 296 B |
| NSubstitute | 659.0 ns | 297.12 ns | 16.29 ns | 272 B |
| FakeItEasy | 1,726.3 ns | 385.80 ns | 21.15 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2072
  bar [177.2, 309.1, 110.2, 581.4, 659, 1726.3]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,691.3 ns | 6,011.40 ns | 329.51 ns | 12736 B |
| Imposter | 30,976.6 ns | 6,206.44 ns | 340.20 ns | 16800 B |
| Mockolate | 12,302.5 ns | 2,645.62 ns | 145.02 ns | 8400 B |
| Moq | 85,674.1 ns | 28,838.26 ns | 1,580.72 ns | 37600 B |
| NSubstitute | 83,159.3 ns | 16,804.71 ns | 921.12 ns | 36448 B |
| FakeItEasy | 195,565.2 ns | 142,942.10 ns | 7,835.14 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 234679
  bar [27691.3, 30976.6, 12302.5, 85674.1, 83159.3, 195565.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-11T02:38:48.126Z*
