---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 254.00 ns | 49.33 ns | 2.704 ns | 120 B |
| Imposter | 310.19 ns | 115.77 ns | 6.346 ns | 168 B |
| Mockolate | 122.12 ns | 63.50 ns | 3.481 ns | 84 B |
| Moq | 843.70 ns | 38.24 ns | 2.096 ns | 376 B |
| NSubstitute | 754.69 ns | 568.80 ns | 31.178 ns | 304 B |
| FakeItEasy | 1,711.93 ns | 351.17 ns | 19.249 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2055
  bar [254, 310.19, 122.12, 843.7, 754.69, 1711.93]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 156.52 ns | 63.01 ns | 3.454 ns | 88 B |
| Imposter | 323.98 ns | 406.85 ns | 22.301 ns | 168 B |
| Mockolate | 93.11 ns | 65.55 ns | 3.593 ns | 60 B |
| Moq | 554.61 ns | 80.23 ns | 4.398 ns | 296 B |
| NSubstitute | 624.18 ns | 181.17 ns | 9.930 ns | 272 B |
| FakeItEasy | 1,619.90 ns | 37.09 ns | 2.033 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1944
  bar [156.52, 323.98, 93.11, 554.61, 624.18, 1619.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,687.86 ns | 9,867.74 ns | 540.884 ns | 11936 B |
| Imposter | 30,081.21 ns | 14,936.81 ns | 818.737 ns | 16800 B |
| Mockolate | 11,914.68 ns | 5,174.40 ns | 283.626 ns | 8400 B |
| Moq | 81,013.71 ns | 7,870.19 ns | 431.391 ns | 37600 B |
| NSubstitute | 77,721.39 ns | 31,972.53 ns | 1,752.522 ns | 36448 B |
| FakeItEasy | 187,910.29 ns | 140,013.97 ns | 7,674.638 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 225493
  bar [25687.86, 30081.21, 11914.68, 81013.71, 77721.39, 187910.29]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-02T03:24:38.193Z*
