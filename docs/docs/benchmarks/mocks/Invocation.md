---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 276.24 ns | 82.604 ns | 4.528 ns | 128 B |
| Imposter | 295.94 ns | 39.360 ns | 2.157 ns | 168 B |
| Mockolate | 109.10 ns | 65.020 ns | 3.564 ns | 84 B |
| Moq | 824.98 ns | 400.844 ns | 21.972 ns | 376 B |
| NSubstitute | 721.45 ns | 108.879 ns | 5.968 ns | 304 B |
| FakeItEasy | 1,704.39 ns | 746.298 ns | 40.907 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2046
  bar [276.24, 295.94, 109.1, 824.98, 721.45, 1704.39]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.54 ns | 77.655 ns | 4.257 ns | 96 B |
| Imposter | 298.90 ns | 28.052 ns | 1.538 ns | 168 B |
| Mockolate | 98.40 ns | 97.117 ns | 5.323 ns | 60 B |
| Moq | 540.84 ns | 60.984 ns | 3.343 ns | 296 B |
| NSubstitute | 611.97 ns | 9.098 ns | 0.499 ns | 272 B |
| FakeItEasy | 1,558.14 ns | 289.163 ns | 15.850 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1870
  bar [165.54, 298.9, 98.4, 540.84, 611.97, 1558.14]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,241.50 ns | 10,970.996 ns | 601.357 ns | 12736 B |
| Imposter | 28,971.47 ns | 13,097.164 ns | 717.900 ns | 16800 B |
| Mockolate | 10,481.04 ns | 2,074.579 ns | 113.715 ns | 8400 B |
| Moq | 84,960.37 ns | 15,861.150 ns | 869.403 ns | 37600 B |
| NSubstitute | 77,793.60 ns | 18,028.162 ns | 988.184 ns | 36448 B |
| FakeItEasy | 178,781.82 ns | 100,116.885 ns | 5,487.744 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 214539
  bar [27241.5, 28971.47, 10481.04, 84960.37, 77793.6, 178781.82]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-03T03:22:34.236Z*
