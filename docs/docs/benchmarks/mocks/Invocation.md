---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 253.3 ns | 167.77 ns | 9.20 ns | 120 B |
| Imposter | 299.6 ns | 69.21 ns | 3.79 ns | 168 B |
| Mockolate | 687.1 ns | 119.18 ns | 6.53 ns | 640 B |
| Moq | 805.6 ns | 507.62 ns | 27.82 ns | 376 B |
| NSubstitute | 730.9 ns | 206.56 ns | 11.32 ns | 304 B |
| FakeItEasy | 1,728.6 ns | 161.32 ns | 8.84 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2075
  bar [253.3, 299.6, 687.1, 805.6, 730.9, 1728.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 161.1 ns | 52.49 ns | 2.88 ns | 88 B |
| Imposter | 317.6 ns | 51.89 ns | 2.84 ns | 168 B |
| Mockolate | 558.7 ns | 32.84 ns | 1.80 ns | 520 B |
| Moq | 525.0 ns | 97.28 ns | 5.33 ns | 296 B |
| NSubstitute | 600.3 ns | 199.91 ns | 10.96 ns | 272 B |
| FakeItEasy | 1,562.5 ns | 72.28 ns | 3.96 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1875
  bar [161.1, 317.6, 558.7, 525, 600.3, 1562.5]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,763.0 ns | 11,588.21 ns | 635.19 ns | 11936 B |
| Imposter | 29,448.9 ns | 8,510.09 ns | 466.47 ns | 16800 B |
| Mockolate | 71,439.0 ns | 16,898.37 ns | 926.26 ns | 64000 B |
| Moq | 85,863.6 ns | 66,386.65 ns | 3,638.88 ns | 37600 B |
| NSubstitute | 75,038.3 ns | 20,771.80 ns | 1,138.57 ns | 30848 B |
| FakeItEasy | 180,185.0 ns | 91,293.34 ns | 5,004.10 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 216222
  bar [25763, 29448.9, 71439, 85863.6, 75038.3, 180185]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-24T03:24:24.137Z*
