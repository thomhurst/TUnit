---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 255.7 ns | 68.83 ns | 3.77 ns | 120 B |
| Imposter | 295.6 ns | 132.37 ns | 7.26 ns | 168 B |
| Mockolate | 639.4 ns | 191.72 ns | 10.51 ns | 640 B |
| Moq | 776.3 ns | 479.54 ns | 26.28 ns | 376 B |
| NSubstitute | 781.1 ns | 267.18 ns | 14.65 ns | 360 B |
| FakeItEasy | 1,739.1 ns | 919.04 ns | 50.38 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2087
  bar [255.7, 295.6, 639.4, 776.3, 781.1, 1739.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 152.2 ns | 66.70 ns | 3.66 ns | 88 B |
| Imposter | 295.2 ns | 111.44 ns | 6.11 ns | 168 B |
| Mockolate | 551.2 ns | 417.07 ns | 22.86 ns | 520 B |
| Moq | 539.9 ns | 226.07 ns | 12.39 ns | 296 B |
| NSubstitute | 644.1 ns | 128.84 ns | 7.06 ns | 328 B |
| FakeItEasy | 1,524.2 ns | 345.64 ns | 18.95 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1830
  bar [152.2, 295.2, 551.2, 539.9, 644.1, 1524.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,043.7 ns | 6,857.16 ns | 375.86 ns | 11936 B |
| Imposter | 28,485.7 ns | 6,601.34 ns | 361.84 ns | 16800 B |
| Mockolate | 64,778.2 ns | 17,773.82 ns | 974.24 ns | 64000 B |
| Moq | 78,367.9 ns | 8,195.12 ns | 449.20 ns | 37600 B |
| NSubstitute | 71,134.0 ns | 17,950.88 ns | 983.95 ns | 30848 B |
| FakeItEasy | 170,368.5 ns | 25,202.82 ns | 1,381.45 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 204443
  bar [26043.7, 28485.7, 64778.2, 78367.9, 71134, 170368.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T03:22:20.916Z*
