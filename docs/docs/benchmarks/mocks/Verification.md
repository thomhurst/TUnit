---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 715.93 ns | 8.096 ns | 7.573 ns | 3008 B |
| Imposter | 663.18 ns | 13.298 ns | 18.203 ns | 4688 B |
| Mockolate | 426.50 ns | 7.035 ns | 7.820 ns | 2128 B |
| Moq | 349,616.36 ns | 3,034.807 ns | 2,838.761 ns | 24325 B |
| NSubstitute | 6,322.41 ns | 62.580 ns | 55.476 ns | 10064 B |
| FakeItEasy | 7,513.14 ns | 60.801 ns | 50.772 ns | 10722 B |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 419540
  bar [715.93, 663.18, 426.5, 349616.36, 6322.41, 7513.14]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.76 ns | 1.073 ns | 1.278 ns | 320 B |
| Imposter | 305.87 ns | 2.250 ns | 1.994 ns | 2400 B |
| Mockolate | 231.07 ns | 1.514 ns | 1.416 ns | 1144 B |
| Moq | 88,564.13 ns | 249.255 ns | 220.958 ns | 6918 B |
| NSubstitute | 3,667.71 ns | 44.035 ns | 41.191 ns | 7088 B |
| FakeItEasy | 3,678.84 ns | 35.820 ns | 29.912 ns | 5210 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 106277
  bar [52.76, 305.87, 231.07, 88564.13, 3667.71, 3678.84]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,227.78 ns | 24.121 ns | 22.563 ns | 4472 B |
| Imposter | 1,787.81 ns | 34.641 ns | 50.776 ns | 11192 B |
| Mockolate | 1,115.71 ns | 11.108 ns | 8.672 ns | 5240 B |
| Moq | 478,640.59 ns | 1,913.831 ns | 1,696.561 ns | 34699 B |
| NSubstitute | 11,611.44 ns | 65.263 ns | 54.497 ns | 16929 B |
| FakeItEasy | 14,538.89 ns | 138.156 ns | 122.472 ns | 19313 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 574369
  bar [1227.78, 1787.81, 1115.71, 478640.59, 11611.44, 14538.89]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-02T03:26:25.775Z*
