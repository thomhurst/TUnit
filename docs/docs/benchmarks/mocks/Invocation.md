---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 274.3 ns | 75.55 ns | 4.14 ns | 128 B |
| Imposter | 299.5 ns | 102.36 ns | 5.61 ns | 168 B |
| Mockolate | 117.5 ns | 52.29 ns | 2.87 ns | 84 B |
| Moq | 821.8 ns | 142.29 ns | 7.80 ns | 376 B |
| NSubstitute | 825.1 ns | 124.63 ns | 6.83 ns | 360 B |
| FakeItEasy | 1,795.4 ns | 147.16 ns | 8.07 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2155
  bar [274.3, 299.5, 117.5, 821.8, 825.1, 1795.4]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.5 ns | 69.73 ns | 3.82 ns | 96 B |
| Imposter | 297.2 ns | 95.15 ns | 5.22 ns | 168 B |
| Mockolate | 101.7 ns | 15.39 ns | 0.84 ns | 60 B |
| Moq | 548.9 ns | 193.25 ns | 10.59 ns | 296 B |
| NSubstitute | 627.2 ns | 224.86 ns | 12.33 ns | 272 B |
| FakeItEasy | 1,614.7 ns | 88.34 ns | 4.84 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1938
  bar [164.5, 297.2, 101.7, 548.9, 627.2, 1614.7]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,297.5 ns | 10,852.62 ns | 594.87 ns | 12736 B |
| Imposter | 29,287.7 ns | 8,378.59 ns | 459.26 ns | 16800 B |
| Mockolate | 11,598.6 ns | 1,314.49 ns | 72.05 ns | 8400 B |
| Moq | 82,994.6 ns | 17,657.66 ns | 967.88 ns | 37600 B |
| NSubstitute | 74,564.6 ns | 20,528.69 ns | 1,125.25 ns | 30848 B |
| FakeItEasy | 188,157.0 ns | 90,523.64 ns | 4,961.91 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 225789
  bar [27297.5, 29287.7, 11598.6, 82994.6, 74564.6, 188157]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-22T03:30:58.892Z*
