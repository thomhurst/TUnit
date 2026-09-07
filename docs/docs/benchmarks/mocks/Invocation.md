---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 272.86 ns | 130.16 ns | 7.135 ns | 128 B |
| Imposter | 289.99 ns | 64.60 ns | 3.541 ns | 168 B |
| Mockolate | 102.96 ns | 16.53 ns | 0.906 ns | 84 B |
| Moq | 783.40 ns | 77.08 ns | 4.225 ns | 376 B |
| NSubstitute | 718.18 ns | 220.84 ns | 12.105 ns | 304 B |
| FakeItEasy | 1,690.60 ns | 510.78 ns | 27.997 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2029
  bar [272.86, 289.99, 102.96, 783.4, 718.18, 1690.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.34 ns | 74.42 ns | 4.079 ns | 96 B |
| Imposter | 290.64 ns | 62.30 ns | 3.415 ns | 168 B |
| Mockolate | 93.38 ns | 12.80 ns | 0.701 ns | 60 B |
| Moq | 515.06 ns | 167.01 ns | 9.154 ns | 296 B |
| NSubstitute | 607.99 ns | 204.04 ns | 11.184 ns | 272 B |
| FakeItEasy | 1,532.60 ns | 342.61 ns | 18.780 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1840
  bar [164.34, 290.64, 93.38, 515.06, 607.99, 1532.6]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,851.62 ns | 11,217.29 ns | 614.857 ns | 12736 B |
| Imposter | 28,832.42 ns | 8,108.09 ns | 444.432 ns | 16800 B |
| Mockolate | 10,184.50 ns | 1,835.80 ns | 100.626 ns | 8400 B |
| Moq | 78,426.76 ns | 9,523.10 ns | 521.994 ns | 37600 B |
| NSubstitute | 70,037.52 ns | 14,254.93 ns | 781.361 ns | 30848 B |
| FakeItEasy | 174,019.15 ns | 21,491.02 ns | 1,177.996 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208823
  bar [26851.62, 28832.42, 10184.5, 78426.76, 70037.52, 174019.15]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-07T02:34:20.667Z*
