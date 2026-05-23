---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 267.09 ns | 70.93 ns | 3.888 ns | 120 B |
| Imposter | 303.44 ns | 45.40 ns | 2.488 ns | 168 B |
| Mockolate | 125.19 ns | 47.60 ns | 2.609 ns | 84 B |
| Moq | 854.16 ns | 184.84 ns | 10.132 ns | 376 B |
| NSubstitute | 773.80 ns | 253.15 ns | 13.876 ns | 304 B |
| FakeItEasy | 1,683.09 ns | 403.74 ns | 22.130 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2020
  bar [267.09, 303.44, 125.19, 854.16, 773.8, 1683.09]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 160.09 ns | 81.23 ns | 4.452 ns | 88 B |
| Imposter | 298.88 ns | 118.33 ns | 6.486 ns | 168 B |
| Mockolate | 96.36 ns | 63.67 ns | 3.490 ns | 60 B |
| Moq | 560.00 ns | 77.13 ns | 4.228 ns | 296 B |
| NSubstitute | 645.05 ns | 323.77 ns | 17.747 ns | 272 B |
| FakeItEasy | 1,593.08 ns | 601.17 ns | 32.952 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1912
  bar [160.09, 298.88, 96.36, 560, 645.05, 1593.08]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,385.27 ns | 13,318.32 ns | 730.022 ns | 11936 B |
| Imposter | 29,093.10 ns | 5,316.61 ns | 291.421 ns | 16800 B |
| Mockolate | 10,208.56 ns | 3,017.63 ns | 165.407 ns | 8400 B |
| Moq | 76,309.33 ns | 20,951.72 ns | 1,148.434 ns | 37600 B |
| NSubstitute | 69,871.25 ns | 20,163.48 ns | 1,105.229 ns | 30848 B |
| FakeItEasy | 172,156.28 ns | 70,904.67 ns | 3,886.524 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 206588
  bar [26385.27, 29093.1, 10208.56, 76309.33, 69871.25, 172156.28]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-23T03:25:20.859Z*
