---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 277.1 ns | 90.73 ns | 4.97 ns | 128 B |
| Imposter | 307.6 ns | 89.32 ns | 4.90 ns | 168 B |
| Mockolate | 131.0 ns | 13.68 ns | 0.75 ns | 84 B |
| Moq | 858.3 ns | 178.49 ns | 9.78 ns | 376 B |
| NSubstitute | 782.7 ns | 115.60 ns | 6.34 ns | 304 B |
| FakeItEasy | 1,898.3 ns | 391.94 ns | 21.48 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2278
  bar [277.1, 307.6, 131, 858.3, 782.7, 1898.3]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 174.0 ns | 68.83 ns | 3.77 ns | 96 B |
| Imposter | 307.0 ns | 85.60 ns | 4.69 ns | 168 B |
| Mockolate | 110.9 ns | 87.35 ns | 4.79 ns | 60 B |
| Moq | 598.0 ns | 88.51 ns | 4.85 ns | 296 B |
| NSubstitute | 657.3 ns | 183.33 ns | 10.05 ns | 272 B |
| FakeItEasy | 1,750.2 ns | 365.12 ns | 20.01 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2101
  bar [174, 307, 110.9, 598, 657.3, 1750.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,303.0 ns | 14,474.87 ns | 793.42 ns | 12736 B |
| Imposter | 30,449.7 ns | 10,226.73 ns | 560.56 ns | 16800 B |
| Mockolate | 13,836.3 ns | 14,965.49 ns | 820.31 ns | 8400 B |
| Moq | 85,952.0 ns | 12,637.13 ns | 692.68 ns | 37600 B |
| NSubstitute | 77,211.5 ns | 18,951.02 ns | 1,038.77 ns | 30848 B |
| FakeItEasy | 196,037.8 ns | 42,583.70 ns | 2,334.16 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 235246
  bar [28303, 30449.7, 13836.3, 85952, 77211.5, 196037.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-09T03:24:10.827Z*
