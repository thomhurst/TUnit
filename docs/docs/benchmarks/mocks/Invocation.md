---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 266.37 ns | 70.95 ns | 3.889 ns | 120 B |
| Imposter | 299.92 ns | 18.43 ns | 1.010 ns | 168 B |
| Mockolate | 127.28 ns | 14.44 ns | 0.791 ns | 84 B |
| Moq | 819.40 ns | 489.19 ns | 26.814 ns | 376 B |
| NSubstitute | 745.44 ns | 131.71 ns | 7.220 ns | 304 B |
| FakeItEasy | 2,031.36 ns | 454.12 ns | 24.892 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2438
  bar [266.37, 299.92, 127.28, 819.4, 745.44, 2031.36]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 160.37 ns | 71.06 ns | 3.895 ns | 88 B |
| Imposter | 299.72 ns | 172.64 ns | 9.463 ns | 168 B |
| Mockolate | 95.72 ns | 74.38 ns | 4.077 ns | 60 B |
| Moq | 514.31 ns | 62.90 ns | 3.448 ns | 296 B |
| NSubstitute | 601.13 ns | 262.05 ns | 14.364 ns | 272 B |
| FakeItEasy | 1,485.52 ns | 106.14 ns | 5.818 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1783
  bar [160.37, 299.72, 95.72, 514.31, 601.13, 1485.52]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,979.39 ns | 9,532.60 ns | 522.514 ns | 11936 B |
| Imposter | 29,063.61 ns | 10,948.92 ns | 600.147 ns | 16800 B |
| Mockolate | 9,783.73 ns | 1,625.23 ns | 89.084 ns | 8400 B |
| Moq | 78,221.85 ns | 21,526.57 ns | 1,179.944 ns | 37600 B |
| NSubstitute | 69,453.97 ns | 13,579.64 ns | 744.346 ns | 30848 B |
| FakeItEasy | 167,866.12 ns | 44,812.34 ns | 2,456.315 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 201440
  bar [25979.39, 29063.61, 9783.73, 78221.85, 69453.97, 167866.12]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-26T03:27:58.119Z*
