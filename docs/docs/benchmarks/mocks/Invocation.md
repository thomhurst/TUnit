---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 315.9 ns | 78.44 ns | 4.30 ns | 128 B |
| Imposter | 307.5 ns | 42.62 ns | 2.34 ns | 168 B |
| Mockolate | 117.0 ns | 171.07 ns | 9.38 ns | 84 B |
| Moq | 846.6 ns | 125.29 ns | 6.87 ns | 376 B |
| NSubstitute | 749.0 ns | 127.83 ns | 7.01 ns | 304 B |
| FakeItEasy | 1,817.9 ns | 383.20 ns | 21.00 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2182
  bar [315.9, 307.5, 117, 846.6, 749, 1817.9]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 167.0 ns | 84.60 ns | 4.64 ns | 96 B |
| Imposter | 297.5 ns | 76.30 ns | 4.18 ns | 168 B |
| Mockolate | 101.2 ns | 90.67 ns | 4.97 ns | 60 B |
| Moq | 555.1 ns | 322.27 ns | 17.66 ns | 296 B |
| NSubstitute | 638.0 ns | 390.24 ns | 21.39 ns | 272 B |
| FakeItEasy | 1,633.0 ns | 507.33 ns | 27.81 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1960
  bar [167, 297.5, 101.2, 555.1, 638, 1633]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,810.8 ns | 14,072.34 ns | 771.35 ns | 12736 B |
| Imposter | 29,526.5 ns | 11,660.64 ns | 639.16 ns | 16800 B |
| Mockolate | 11,256.6 ns | 2,565.51 ns | 140.62 ns | 8400 B |
| Moq | 84,486.8 ns | 14,057.91 ns | 770.56 ns | 37600 B |
| NSubstitute | 74,080.0 ns | 16,633.03 ns | 911.71 ns | 30848 B |
| FakeItEasy | 183,407.7 ns | 81,131.32 ns | 4,447.08 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 220090
  bar [27810.8, 29526.5, 11256.6, 84486.8, 74080, 183407.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-30T03:21:07.533Z*
