---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 261.8 ns | 49.48 ns | 2.71 ns | 120 B |
| Imposter | 316.7 ns | 41.66 ns | 2.28 ns | 168 B |
| Mockolate | 150.1 ns | 131.86 ns | 7.23 ns | 84 B |
| Moq | 911.5 ns | 201.52 ns | 11.05 ns | 376 B |
| NSubstitute | 788.5 ns | 306.51 ns | 16.80 ns | 304 B |
| FakeItEasy | 1,934.1 ns | 404.26 ns | 22.16 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2321
  bar [261.8, 316.7, 150.1, 911.5, 788.5, 1934.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 158.2 ns | 72.08 ns | 3.95 ns | 88 B |
| Imposter | 320.8 ns | 170.07 ns | 9.32 ns | 168 B |
| Mockolate | 117.6 ns | 24.32 ns | 1.33 ns | 60 B |
| Moq | 622.5 ns | 268.24 ns | 14.70 ns | 296 B |
| NSubstitute | 654.8 ns | 188.65 ns | 10.34 ns | 272 B |
| FakeItEasy | 1,763.2 ns | 275.93 ns | 15.12 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2116
  bar [158.2, 320.8, 117.6, 622.5, 654.8, 1763.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,237.5 ns | 12,980.56 ns | 711.51 ns | 11936 B |
| Imposter | 30,549.7 ns | 9,876.33 ns | 541.35 ns | 16800 B |
| Mockolate | 13,344.3 ns | 3,030.53 ns | 166.11 ns | 8400 B |
| Moq | 83,209.8 ns | 5,937.43 ns | 325.45 ns | 37600 B |
| NSubstitute | 75,716.9 ns | 27,067.40 ns | 1,483.66 ns | 30848 B |
| FakeItEasy | 194,774.8 ns | 77,467.38 ns | 4,246.25 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 233730
  bar [26237.5, 30549.7, 13344.3, 83209.8, 75716.9, 194774.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-03T03:31:53.295Z*
