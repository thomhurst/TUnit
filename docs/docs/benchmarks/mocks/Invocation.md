---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects with bounded history; time and allocation are reported per call — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects with bounded history; time and allocation are reported per call — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Calling methods on mock objects with bounded history; time and allocation are reported per call:

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 62.37 ns | 142.389 ns | 7.805 ns | 101 B |
| Imposter | 16.00 ns | 9.377 ns | 0.514 ns | 48 B |
| Mockolate | 23.92 ns | 4.097 ns | 0.225 ns | 56 B |
| Moq | 202.57 ns | 15.180 ns | 0.832 ns | 396 B |
| NSubstitute | 146.58 ns | 28.872 ns | 1.583 ns | 309 B |
| FakeItEasy | 256.47 ns | 39.428 ns | 2.161 ns | 981 B |

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
  y-axis "Time (ns)" 0 --> 308
  bar [62.37, 16, 23.92, 202.57, 146.58, 256.47]
```

---

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.23 ns | 2.900 ns | 0.159 ns | 108 B |
| Imposter | 15.47 ns | 8.484 ns | 0.465 ns | 48 B |
| Mockolate | 17.81 ns | 17.541 ns | 0.961 ns | 56 B |
| Moq | 200.10 ns | 13.981 ns | 0.766 ns | 392 B |
| NSubstitute | 177.77 ns | 75.407 ns | 4.133 ns | 376 B |
| FakeItEasy | 250.24 ns | 44.378 ns | 2.433 ns | 992 B |

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
  y-axis "Time (ns)" 0 --> 301
  bar [57.23, 15.47, 17.81, 200.1, 177.77, 250.24]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.21 ns | 0.807 ns | 0.044 ns | 98 B |
| Imposter | 13.91 ns | 1.331 ns | 0.073 ns | 48 B |
| Mockolate | 18.69 ns | 13.113 ns | 0.719 ns | 56 B |
| Moq | 101.66 ns | 39.392 ns | 2.159 ns | 312 B |
| NSubstitute | 150.60 ns | 11.454 ns | 0.628 ns | 344 B |
| FakeItEasy | 211.81 ns | 64.148 ns | 3.516 ns | 832 B |

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
  y-axis "Time (ns)" 0 --> 255
  bar [50.21, 13.91, 18.69, 101.66, 150.6, 211.81]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for calling methods on mock objects with bounded history; time and allocation are reported per call.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-23T02:22:45.286Z*
