---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 278.80 ns | 108.27 ns | 5.935 ns | 128 B |
| Imposter | 302.20 ns | 86.72 ns | 4.754 ns | 168 B |
| Mockolate | 116.19 ns | 34.63 ns | 1.898 ns | 84 B |
| Moq | 807.41 ns | 318.36 ns | 17.450 ns | 376 B |
| NSubstitute | 720.58 ns | 361.94 ns | 19.839 ns | 304 B |
| FakeItEasy | 1,802.93 ns | 95.33 ns | 5.226 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2164
  bar [278.8, 302.2, 116.19, 807.41, 720.58, 1802.93]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 170.31 ns | 74.67 ns | 4.093 ns | 96 B |
| Imposter | 298.67 ns | 65.81 ns | 3.607 ns | 168 B |
| Mockolate | 94.48 ns | 29.45 ns | 1.614 ns | 60 B |
| Moq | 559.29 ns | 82.34 ns | 4.513 ns | 296 B |
| NSubstitute | 621.08 ns | 286.36 ns | 15.697 ns | 272 B |
| FakeItEasy | 1,743.22 ns | 425.36 ns | 23.316 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2092
  bar [170.31, 298.67, 94.48, 559.29, 621.08, 1743.22]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,191.63 ns | 8,753.65 ns | 479.817 ns | 12736 B |
| Imposter | 30,344.16 ns | 10,768.12 ns | 590.237 ns | 16800 B |
| Mockolate | 10,439.80 ns | 4,975.96 ns | 272.749 ns | 8400 B |
| Moq | 80,994.18 ns | 22,082.13 ns | 1,210.396 ns | 37600 B |
| NSubstitute | 71,527.35 ns | 9,287.19 ns | 509.062 ns | 30848 B |
| FakeItEasy | 171,798.14 ns | 30,042.68 ns | 1,646.741 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 206158
  bar [28191.63, 30344.16, 10439.8, 80994.18, 71527.35, 171798.14]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-15T02:39:16.112Z*
