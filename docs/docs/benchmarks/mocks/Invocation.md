---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 280.1 ns | 84.41 ns | 4.63 ns | 128 B |
| Imposter | 302.1 ns | 78.25 ns | 4.29 ns | 168 B |
| Mockolate | 120.4 ns | 18.01 ns | 0.99 ns | 84 B |
| Moq | 842.1 ns | 63.06 ns | 3.46 ns | 376 B |
| NSubstitute | 741.9 ns | 183.14 ns | 10.04 ns | 304 B |
| FakeItEasy | 1,833.4 ns | 1,040.60 ns | 57.04 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2201
  bar [280.1, 302.1, 120.4, 842.1, 741.9, 1833.4]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 167.6 ns | 101.26 ns | 5.55 ns | 96 B |
| Imposter | 304.7 ns | 92.62 ns | 5.08 ns | 168 B |
| Mockolate | 111.1 ns | 75.97 ns | 4.16 ns | 60 B |
| Moq | 578.3 ns | 5.57 ns | 0.31 ns | 296 B |
| NSubstitute | 647.7 ns | 119.54 ns | 6.55 ns | 272 B |
| FakeItEasy | 1,656.2 ns | 139.27 ns | 7.63 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1988
  bar [167.6, 304.7, 111.1, 578.3, 647.7, 1656.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,075.3 ns | 12,229.77 ns | 670.35 ns | 12736 B |
| Imposter | 30,043.1 ns | 1,090.07 ns | 59.75 ns | 16800 B |
| Mockolate | 12,232.2 ns | 11,831.43 ns | 648.52 ns | 8400 B |
| Moq | 85,801.2 ns | 6,953.11 ns | 381.12 ns | 37600 B |
| NSubstitute | 84,752.3 ns | 12,317.50 ns | 675.16 ns | 36448 B |
| FakeItEasy | 198,538.8 ns | 103,123.03 ns | 5,652.52 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 238247
  bar [28075.3, 30043.1, 12232.2, 85801.2, 84752.3, 198538.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-19T03:27:20.624Z*
