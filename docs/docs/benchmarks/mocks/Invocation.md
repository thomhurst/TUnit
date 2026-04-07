---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 261.5 ns | 85.01 ns | 4.66 ns | 120 B |
| Imposter | 296.5 ns | 100.09 ns | 5.49 ns | 168 B |
| Mockolate | 707.3 ns | 238.12 ns | 13.05 ns | 640 B |
| Moq | 838.3 ns | 187.87 ns | 10.30 ns | 376 B |
| NSubstitute | 752.6 ns | 191.36 ns | 10.49 ns | 304 B |
| FakeItEasy | 1,883.0 ns | 949.75 ns | 52.06 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2260
  bar [261.5, 296.5, 707.3, 838.3, 752.6, 1883]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 157.6 ns | 68.10 ns | 3.73 ns | 88 B |
| Imposter | 302.9 ns | 87.59 ns | 4.80 ns | 168 B |
| Mockolate | 551.7 ns | 483.54 ns | 26.50 ns | 520 B |
| Moq | 547.3 ns | 352.10 ns | 19.30 ns | 296 B |
| NSubstitute | 594.6 ns | 152.85 ns | 8.38 ns | 272 B |
| FakeItEasy | 1,584.9 ns | 360.99 ns | 19.79 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1902
  bar [157.6, 302.9, 551.7, 547.3, 594.6, 1584.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,713.5 ns | 8,104.61 ns | 444.24 ns | 11936 B |
| Imposter | 28,487.4 ns | 10,084.31 ns | 552.76 ns | 16800 B |
| Mockolate | 66,299.9 ns | 33,448.67 ns | 1,833.43 ns | 64000 B |
| Moq | 79,836.2 ns | 20,420.16 ns | 1,119.30 ns | 37600 B |
| NSubstitute | 74,687.4 ns | 39,403.14 ns | 2,159.82 ns | 30848 B |
| FakeItEasy | 182,809.1 ns | 61,051.13 ns | 3,346.42 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 219371
  bar [25713.5, 28487.4, 66299.9, 79836.2, 74687.4, 182809.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-07T03:21:31.527Z*
