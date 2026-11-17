---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 282.52 ns | 356.22 ns | 19.525 ns | 128 B |
| Imposter | 79.14 ns | 96.07 ns | 5.266 ns | 48 B |
| Mockolate | 78.49 ns | 72.72 ns | 3.986 ns | 34 B |
| Moq | 909.17 ns | 1,080.10 ns | 59.204 ns | 376 B |
| NSubstitute | 688.53 ns | 1,382.90 ns | 75.801 ns | 304 B |
| FakeItEasy | 2,014.70 ns | 4,130.23 ns | 226.392 ns | 960 B |

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
  y-axis "Time (ns)" 0 --> 2418
  bar [282.52, 79.14, 78.49, 909.17, 688.53, 2014.7]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 234.71 ns | 633.65 ns | 34.733 ns | 96 B |
| Imposter | 82.36 ns | 139.28 ns | 7.634 ns | 48 B |
| Mockolate | 80.92 ns | 55.19 ns | 3.025 ns | 34 B |
| Moq | 549.62 ns | 706.66 ns | 38.734 ns | 296 B |
| NSubstitute | 617.85 ns | 1,553.11 ns | 85.131 ns | 272 B |
| FakeItEasy | 1,847.52 ns | 2,773.30 ns | 152.014 ns | 800 B |

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
  y-axis "Time (ns)" 0 --> 2218
  bar [234.71, 82.36, 80.92, 549.62, 617.85, 1847.52]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 32,078.44 ns | 108,424.39 ns | 5,943.107 ns | 13248 B |
| Imposter | 8,244.86 ns | 11,372.95 ns | 623.390 ns | 4800 B |
| Mockolate | 8,023.81 ns | 6,286.24 ns | 344.570 ns | 3350 B |
| Moq | 75,615.50 ns | 138,442.52 ns | 7,588.502 ns | 37600 B |
| NSubstitute | 73,780.07 ns | 183,594.87 ns | 10,063.455 ns | 36448 B |
| FakeItEasy | 167,226.67 ns | 342,072.19 ns | 18,750.131 ns | 96000 B |

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
  y-axis "Time (ns)" 0 --> 200673
  bar [32078.44, 8244.86, 8023.81, 75615.5, 73780.07, 167226.67]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-17T02:23:59.645Z*
