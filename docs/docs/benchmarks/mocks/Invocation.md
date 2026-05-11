---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 257.67 ns | 82.43 ns | 4.518 ns | 120 B |
| Imposter | 289.64 ns | 89.01 ns | 4.879 ns | 168 B |
| Mockolate | 105.32 ns | 115.05 ns | 6.307 ns | 84 B |
| Moq | 797.14 ns | 195.18 ns | 10.699 ns | 376 B |
| NSubstitute | 718.17 ns | 300.61 ns | 16.478 ns | 304 B |
| FakeItEasy | 1,705.01 ns | 941.78 ns | 51.622 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2047
  bar [257.67, 289.64, 105.32, 797.14, 718.17, 1705.01]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 151.90 ns | 76.07 ns | 4.170 ns | 88 B |
| Imposter | 294.57 ns | 86.11 ns | 4.720 ns | 168 B |
| Mockolate | 95.42 ns | 21.28 ns | 1.166 ns | 60 B |
| Moq | 540.50 ns | 178.33 ns | 9.775 ns | 296 B |
| NSubstitute | 616.85 ns | 297.63 ns | 16.314 ns | 272 B |
| FakeItEasy | 1,557.33 ns | 669.41 ns | 36.693 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1869
  bar [151.9, 294.57, 95.42, 540.5, 616.85, 1557.33]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,569.66 ns | 11,995.88 ns | 657.535 ns | 11936 B |
| Imposter | 28,800.88 ns | 8,004.43 ns | 438.750 ns | 16800 B |
| Mockolate | 10,588.02 ns | 6,946.61 ns | 380.767 ns | 8400 B |
| Moq | 80,453.19 ns | 2,704.90 ns | 148.265 ns | 37600 B |
| NSubstitute | 72,980.61 ns | 27,671.04 ns | 1,516.743 ns | 30848 B |
| FakeItEasy | 175,261.43 ns | 95,710.60 ns | 5,246.221 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 210314
  bar [25569.66, 28800.88, 10588.02, 80453.19, 72980.61, 175261.43]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-11T03:29:06.162Z*
