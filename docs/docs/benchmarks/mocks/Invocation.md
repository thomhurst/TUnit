---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 259.7 ns | 65.67 ns | 3.60 ns | 120 B |
| Imposter | 297.2 ns | 68.13 ns | 3.73 ns | 168 B |
| Mockolate | 680.4 ns | 19.50 ns | 1.07 ns | 640 B |
| Moq | 782.6 ns | 89.93 ns | 4.93 ns | 376 B |
| NSubstitute | 706.9 ns | 257.30 ns | 14.10 ns | 304 B |
| FakeItEasy | 1,719.1 ns | 122.84 ns | 6.73 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2063
  bar [259.7, 297.2, 680.4, 782.6, 706.9, 1719.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 166.1 ns | 56.39 ns | 3.09 ns | 88 B |
| Imposter | 309.4 ns | 69.84 ns | 3.83 ns | 168 B |
| Mockolate | 543.9 ns | 31.63 ns | 1.73 ns | 520 B |
| Moq | 523.5 ns | 172.81 ns | 9.47 ns | 296 B |
| NSubstitute | 596.4 ns | 246.23 ns | 13.50 ns | 272 B |
| FakeItEasy | 1,532.7 ns | 95.77 ns | 5.25 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1840
  bar [166.1, 309.4, 543.9, 523.5, 596.4, 1532.7]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,014.3 ns | 11,995.41 ns | 657.51 ns | 11936 B |
| Imposter | 29,416.7 ns | 6,552.08 ns | 359.14 ns | 16800 B |
| Mockolate | 68,405.4 ns | 23,561.51 ns | 1,291.49 ns | 64000 B |
| Moq | 78,376.5 ns | 24,752.10 ns | 1,356.75 ns | 37600 B |
| NSubstitute | 74,631.6 ns | 9,225.87 ns | 505.70 ns | 36448 B |
| FakeItEasy | 172,635.0 ns | 56,424.36 ns | 3,092.81 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 207162
  bar [26014.3, 29416.7, 68405.4, 78376.5, 74631.6, 172635]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-18T03:21:40.293Z*
