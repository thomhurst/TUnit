---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 259.4 ns | 57.85 ns | 3.17 ns | 120 B |
| Imposter | 290.6 ns | 86.50 ns | 4.74 ns | 168 B |
| Mockolate | 654.9 ns | 290.38 ns | 15.92 ns | 640 B |
| Moq | 798.8 ns | 128.00 ns | 7.02 ns | 376 B |
| NSubstitute | 722.6 ns | 231.12 ns | 12.67 ns | 304 B |
| FakeItEasy | 1,821.0 ns | 129.51 ns | 7.10 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2186
  bar [259.4, 290.6, 654.9, 798.8, 722.6, 1821]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 156.6 ns | 71.32 ns | 3.91 ns | 88 B |
| Imposter | 289.9 ns | 127.79 ns | 7.00 ns | 168 B |
| Mockolate | 557.0 ns | 176.21 ns | 9.66 ns | 520 B |
| Moq | 535.6 ns | 246.13 ns | 13.49 ns | 296 B |
| NSubstitute | 604.4 ns | 202.96 ns | 11.13 ns | 272 B |
| FakeItEasy | 1,521.0 ns | 358.63 ns | 19.66 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1826
  bar [156.6, 289.9, 557, 535.6, 604.4, 1521]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,886.7 ns | 12,011.28 ns | 658.38 ns | 11936 B |
| Imposter | 28,410.7 ns | 12,497.35 ns | 685.02 ns | 16800 B |
| Mockolate | 63,565.8 ns | 23,792.45 ns | 1,304.14 ns | 64000 B |
| Moq | 80,565.9 ns | 33,918.97 ns | 1,859.21 ns | 37600 B |
| NSubstitute | 69,181.8 ns | 12,592.86 ns | 690.26 ns | 30848 B |
| FakeItEasy | 169,550.2 ns | 36,959.02 ns | 2,025.85 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 203461
  bar [25886.7, 28410.7, 63565.8, 80565.9, 69181.8, 169550.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-15T03:22:40.574Z*
