---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 271.37 ns | 106.76 ns | 5.852 ns | 128 B |
| Imposter | 288.74 ns | 73.69 ns | 4.039 ns | 168 B |
| Mockolate | 116.81 ns | 43.95 ns | 2.409 ns | 84 B |
| Moq | 799.81 ns | 77.15 ns | 4.229 ns | 376 B |
| NSubstitute | 709.34 ns | 140.70 ns | 7.712 ns | 304 B |
| FakeItEasy | 1,711.52 ns | 254.12 ns | 13.929 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2054
  bar [271.37, 288.74, 116.81, 799.81, 709.34, 1711.52]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.89 ns | 83.74 ns | 4.590 ns | 96 B |
| Imposter | 291.19 ns | 147.31 ns | 8.075 ns | 168 B |
| Mockolate | 96.72 ns | 50.19 ns | 2.751 ns | 60 B |
| Moq | 533.41 ns | 160.72 ns | 8.809 ns | 296 B |
| NSubstitute | 607.13 ns | 245.77 ns | 13.472 ns | 272 B |
| FakeItEasy | 1,558.27 ns | 1,296.65 ns | 71.074 ns | 776 B |

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
  bar [164.89, 291.19, 96.72, 533.41, 607.13, 1558.27]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,846.27 ns | 9,662.37 ns | 529.627 ns | 12736 B |
| Imposter | 28,708.89 ns | 12,392.85 ns | 679.294 ns | 16800 B |
| Mockolate | 10,877.33 ns | 15,822.29 ns | 867.273 ns | 8400 B |
| Moq | 79,999.34 ns | 15,089.15 ns | 827.087 ns | 37600 B |
| NSubstitute | 73,949.38 ns | 32,239.63 ns | 1,767.163 ns | 30848 B |
| FakeItEasy | 176,203.34 ns | 48,250.84 ns | 2,644.791 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 211445
  bar [26846.27, 28708.89, 10877.33, 79999.34, 73949.38, 176203.34]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-11T03:26:37.062Z*
