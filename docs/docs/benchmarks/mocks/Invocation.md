---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 340.6 ns | 118.50 ns | 6.50 ns | 128 B |
| Imposter | 364.5 ns | 168.14 ns | 9.22 ns | 168 B |
| Mockolate | 118.5 ns | 46.80 ns | 2.57 ns | 84 B |
| Moq | 912.9 ns | 405.10 ns | 22.21 ns | 376 B |
| NSubstitute | 808.0 ns | 255.44 ns | 14.00 ns | 304 B |
| FakeItEasy | 1,956.8 ns | 658.17 ns | 36.08 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2349
  bar [340.6, 364.5, 118.5, 912.9, 808, 1956.8]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 187.0 ns | 148.02 ns | 8.11 ns | 96 B |
| Imposter | 363.3 ns | 225.64 ns | 12.37 ns | 168 B |
| Mockolate | 112.8 ns | 65.79 ns | 3.61 ns | 60 B |
| Moq | 595.6 ns | 219.81 ns | 12.05 ns | 296 B |
| NSubstitute | 700.0 ns | 362.85 ns | 19.89 ns | 272 B |
| FakeItEasy | 1,734.1 ns | 111.70 ns | 6.12 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2081
  bar [187, 363.3, 112.8, 595.6, 700, 1734.1]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 33,985.1 ns | 30,512.58 ns | 1,672.50 ns | 13248 B |
| Imposter | 35,820.7 ns | 6,350.38 ns | 348.09 ns | 16800 B |
| Mockolate | 12,093.9 ns | 3,181.01 ns | 174.36 ns | 8400 B |
| Moq | 88,578.8 ns | 28,694.38 ns | 1,572.84 ns | 37600 B |
| NSubstitute | 82,302.1 ns | 15,411.83 ns | 844.77 ns | 36448 B |
| FakeItEasy | 214,119.9 ns | 63,525.27 ns | 3,482.03 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 256944
  bar [33985.1, 35820.7, 12093.9, 88578.8, 82302.1, 214119.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-20T03:29:22.484Z*
