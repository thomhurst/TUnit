---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 252.6 ns | 73.04 ns | 4.00 ns | 120 B |
| Imposter | 302.0 ns | 95.02 ns | 5.21 ns | 168 B |
| Mockolate | 686.8 ns | 96.44 ns | 5.29 ns | 640 B |
| Moq | 809.5 ns | 52.00 ns | 2.85 ns | 376 B |
| NSubstitute | 743.5 ns | 302.13 ns | 16.56 ns | 304 B |
| FakeItEasy | 1,769.2 ns | 126.61 ns | 6.94 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2124
  bar [252.6, 302, 686.8, 809.5, 743.5, 1769.2]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 158.7 ns | 62.64 ns | 3.43 ns | 88 B |
| Imposter | 303.2 ns | 66.79 ns | 3.66 ns | 168 B |
| Mockolate | 554.2 ns | 61.70 ns | 3.38 ns | 520 B |
| Moq | 533.8 ns | 252.37 ns | 13.83 ns | 296 B |
| NSubstitute | 596.8 ns | 188.68 ns | 10.34 ns | 272 B |
| FakeItEasy | 1,666.1 ns | 226.87 ns | 12.44 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2000
  bar [158.7, 303.2, 554.2, 533.8, 596.8, 1666.1]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,238.6 ns | 12,145.23 ns | 665.72 ns | 11936 B |
| Imposter | 30,285.1 ns | 13,193.47 ns | 723.18 ns | 16800 B |
| Mockolate | 71,103.9 ns | 27,061.00 ns | 1,483.30 ns | 64000 B |
| Moq | 83,409.0 ns | 124,991.55 ns | 6,851.21 ns | 37600 B |
| NSubstitute | 75,049.3 ns | 28,640.58 ns | 1,569.89 ns | 30848 B |
| FakeItEasy | 178,521.9 ns | 72,034.91 ns | 3,948.48 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 214227
  bar [26238.6, 30285.1, 71103.9, 83409, 75049.3, 178521.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-28T03:25:54.642Z*
