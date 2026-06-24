---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 274.1 ns | 89.61 ns | 4.91 ns | 128 B |
| Imposter | 302.6 ns | 68.15 ns | 3.74 ns | 168 B |
| Mockolate | 125.3 ns | 159.94 ns | 8.77 ns | 84 B |
| Moq | 839.3 ns | 204.72 ns | 11.22 ns | 376 B |
| NSubstitute | 746.8 ns | 311.69 ns | 17.08 ns | 304 B |
| FakeItEasy | 1,835.7 ns | 1,383.60 ns | 75.84 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2203
  bar [274.1, 302.6, 125.3, 839.3, 746.8, 1835.7]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 173.4 ns | 66.09 ns | 3.62 ns | 96 B |
| Imposter | 304.2 ns | 93.16 ns | 5.11 ns | 168 B |
| Mockolate | 106.2 ns | 82.31 ns | 4.51 ns | 60 B |
| Moq | 557.7 ns | 208.81 ns | 11.45 ns | 296 B |
| NSubstitute | 650.9 ns | 226.10 ns | 12.39 ns | 272 B |
| FakeItEasy | 1,623.5 ns | 352.98 ns | 19.35 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1949
  bar [173.4, 304.2, 106.2, 557.7, 650.9, 1623.5]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,244.3 ns | 9,839.50 ns | 539.34 ns | 12736 B |
| Imposter | 29,698.6 ns | 10,929.16 ns | 599.06 ns | 16800 B |
| Mockolate | 12,003.7 ns | 7,153.82 ns | 392.13 ns | 8400 B |
| Moq | 82,207.2 ns | 13,400.80 ns | 734.54 ns | 37600 B |
| NSubstitute | 74,627.2 ns | 10,534.82 ns | 577.45 ns | 30848 B |
| FakeItEasy | 189,640.4 ns | 41,751.26 ns | 2,288.53 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 227569
  bar [27244.3, 29698.6, 12003.7, 82207.2, 74627.2, 189640.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-24T03:28:17.466Z*
