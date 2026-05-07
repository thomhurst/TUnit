---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 252.11 ns | 76.98 ns | 4.220 ns | 120 B |
| Imposter | 287.71 ns | 61.54 ns | 3.373 ns | 168 B |
| Mockolate | 101.11 ns | 39.40 ns | 2.160 ns | 84 B |
| Moq | 785.48 ns | 113.60 ns | 6.227 ns | 376 B |
| NSubstitute | 693.53 ns | 314.80 ns | 17.255 ns | 304 B |
| FakeItEasy | 1,678.21 ns | 253.33 ns | 13.886 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2014
  bar [252.11, 287.71, 101.11, 785.48, 693.53, 1678.21]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 152.16 ns | 64.48 ns | 3.534 ns | 88 B |
| Imposter | 291.60 ns | 92.81 ns | 5.087 ns | 168 B |
| Mockolate | 93.12 ns | 45.83 ns | 2.512 ns | 60 B |
| Moq | 546.32 ns | 42.56 ns | 2.333 ns | 296 B |
| NSubstitute | 614.45 ns | 136.78 ns | 7.498 ns | 272 B |
| FakeItEasy | 1,520.86 ns | 468.61 ns | 25.686 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1826
  bar [152.16, 291.6, 93.12, 546.32, 614.45, 1520.86]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,281.98 ns | 10,902.54 ns | 597.605 ns | 11936 B |
| Imposter | 28,233.68 ns | 8,459.15 ns | 463.674 ns | 16800 B |
| Mockolate | 9,890.21 ns | 2,103.03 ns | 115.274 ns | 8400 B |
| Moq | 79,312.70 ns | 9,428.16 ns | 516.789 ns | 37600 B |
| NSubstitute | 68,918.59 ns | 25,106.46 ns | 1,376.170 ns | 30848 B |
| FakeItEasy | 169,654.16 ns | 96,965.08 ns | 5,314.983 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 203585
  bar [25281.98, 28233.68, 9890.21, 79312.7, 68918.59, 169654.16]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-07T03:27:11.074Z*
