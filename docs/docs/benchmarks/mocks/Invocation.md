---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 404.2 ns | 252.92 ns | 13.86 ns | 192 B |
| Imposter | 375.8 ns | 206.15 ns | 11.30 ns | 168 B |
| Mockolate | 872.2 ns | 163.84 ns | 8.98 ns | 688 B |
| Moq | 933.6 ns | 204.14 ns | 11.19 ns | 376 B |
| NSubstitute | 803.9 ns | 256.95 ns | 14.08 ns | 304 B |
| FakeItEasy | 2,058.4 ns | 501.26 ns | 27.48 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2471
  bar [404.2, 375.8, 872.2, 933.6, 803.9, 2058.4]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 237.3 ns | 8.03 ns | 0.44 ns | 128 B |
| Imposter | 365.5 ns | 165.27 ns | 9.06 ns | 168 B |
| Mockolate | 760.2 ns | 293.25 ns | 16.07 ns | 568 B |
| Moq | 604.7 ns | 175.80 ns | 9.64 ns | 296 B |
| NSubstitute | 700.3 ns | 240.08 ns | 13.16 ns | 272 B |
| FakeItEasy | 1,754.0 ns | 440.00 ns | 24.12 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2105
  bar [237.3, 365.5, 760.2, 604.7, 700.3, 1754]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 41,934.0 ns | 11,359.75 ns | 622.67 ns | 20096 B |
| Imposter | 35,631.0 ns | 7,181.18 ns | 393.62 ns | 16800 B |
| Mockolate | 85,371.9 ns | 22,839.30 ns | 1,251.90 ns | 68800 B |
| Moq | 88,551.5 ns | 40,570.95 ns | 2,223.83 ns | 37600 B |
| NSubstitute | 79,302.4 ns | 42,113.43 ns | 2,308.38 ns | 30848 B |
| FakeItEasy | 206,727.2 ns | 7,237.42 ns | 396.71 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 248073
  bar [41934, 35631, 85371.9, 88551.5, 79302.4, 206727.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T03:24:56.545Z*
