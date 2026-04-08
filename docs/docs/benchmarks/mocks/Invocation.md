---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 256.3 ns | 79.33 ns | 4.35 ns | 120 B |
| Imposter | 293.4 ns | 98.43 ns | 5.40 ns | 168 B |
| Mockolate | 644.1 ns | 144.89 ns | 7.94 ns | 640 B |
| Moq | 808.8 ns | 599.84 ns | 32.88 ns | 376 B |
| NSubstitute | 733.1 ns | 169.93 ns | 9.31 ns | 304 B |
| FakeItEasy | 1,677.3 ns | 185.63 ns | 10.18 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2013
  bar [256.3, 293.4, 644.1, 808.8, 733.1, 1677.3]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 156.4 ns | 66.27 ns | 3.63 ns | 88 B |
| Imposter | 287.3 ns | 107.35 ns | 5.88 ns | 168 B |
| Mockolate | 510.4 ns | 119.33 ns | 6.54 ns | 520 B |
| Moq | 519.4 ns | 243.23 ns | 13.33 ns | 296 B |
| NSubstitute | 603.6 ns | 189.90 ns | 10.41 ns | 272 B |
| FakeItEasy | 1,599.1 ns | 961.14 ns | 52.68 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1919
  bar [156.4, 287.3, 510.4, 519.4, 603.6, 1599.1]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,345.4 ns | 6,687.33 ns | 366.56 ns | 11936 B |
| Imposter | 28,606.6 ns | 12,600.34 ns | 690.67 ns | 16800 B |
| Mockolate | 64,539.3 ns | 27,979.44 ns | 1,533.65 ns | 64000 B |
| Moq | 80,516.4 ns | 35,288.62 ns | 1,934.29 ns | 37600 B |
| NSubstitute | 72,674.0 ns | 17,366.20 ns | 951.90 ns | 30848 B |
| FakeItEasy | 177,835.2 ns | 13,362.99 ns | 732.47 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 213403
  bar [26345.4, 28606.6, 64539.3, 80516.4, 72674, 177835.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-08T03:21:46.624Z*
