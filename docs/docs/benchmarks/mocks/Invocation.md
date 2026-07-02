---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 267.10 ns | 109.11 ns | 5.981 ns | 128 B |
| Imposter | 296.81 ns | 74.57 ns | 4.088 ns | 168 B |
| Mockolate | 106.17 ns | 43.67 ns | 2.393 ns | 84 B |
| Moq | 774.65 ns | 52.53 ns | 2.879 ns | 376 B |
| NSubstitute | 704.56 ns | 187.96 ns | 10.303 ns | 304 B |
| FakeItEasy | 1,712.99 ns | 238.99 ns | 13.100 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2056
  bar [267.1, 296.81, 106.17, 774.65, 704.56, 1712.99]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 172.26 ns | 52.73 ns | 2.890 ns | 96 B |
| Imposter | 295.92 ns | 76.53 ns | 4.195 ns | 168 B |
| Mockolate | 97.84 ns | 15.81 ns | 0.867 ns | 60 B |
| Moq | 578.61 ns | 86.27 ns | 4.729 ns | 296 B |
| NSubstitute | 635.24 ns | 131.52 ns | 7.209 ns | 328 B |
| FakeItEasy | 1,514.42 ns | 78.26 ns | 4.290 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1818
  bar [172.26, 295.92, 97.84, 578.61, 635.24, 1514.42]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,596.33 ns | 11,124.28 ns | 609.760 ns | 12736 B |
| Imposter | 29,291.00 ns | 8,172.05 ns | 447.938 ns | 16800 B |
| Mockolate | 10,461.78 ns | 1,237.50 ns | 67.831 ns | 8400 B |
| Moq | 78,987.21 ns | 20,306.85 ns | 1,113.087 ns | 37600 B |
| NSubstitute | 74,536.91 ns | 12,432.03 ns | 681.441 ns | 36448 B |
| FakeItEasy | 172,585.91 ns | 54,376.31 ns | 2,980.549 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 207104
  bar [26596.33, 29291, 10461.78, 78987.21, 74536.91, 172585.91]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-02T03:26:25.775Z*
