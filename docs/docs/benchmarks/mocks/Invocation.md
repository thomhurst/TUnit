---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 262.50 ns | 77.152 ns | 4.229 ns | 120 B |
| Imposter | 293.31 ns | 57.763 ns | 3.166 ns | 168 B |
| Mockolate | 109.72 ns | 83.823 ns | 4.595 ns | 84 B |
| Moq | 804.58 ns | 70.550 ns | 3.867 ns | 376 B |
| NSubstitute | 706.76 ns | 152.600 ns | 8.365 ns | 304 B |
| FakeItEasy | 1,754.11 ns | 529.673 ns | 29.033 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2105
  bar [262.5, 293.31, 109.72, 804.58, 706.76, 1754.11]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 157.14 ns | 61.336 ns | 3.362 ns | 88 B |
| Imposter | 291.05 ns | 69.382 ns | 3.803 ns | 168 B |
| Mockolate | 94.54 ns | 7.962 ns | 0.436 ns | 60 B |
| Moq | 526.33 ns | 53.632 ns | 2.940 ns | 296 B |
| NSubstitute | 638.24 ns | 335.349 ns | 18.382 ns | 272 B |
| FakeItEasy | 1,543.16 ns | 154.133 ns | 8.449 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1852
  bar [157.14, 291.05, 94.54, 526.33, 638.24, 1543.16]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,067.49 ns | 10,845.727 ns | 594.491 ns | 11936 B |
| Imposter | 28,722.53 ns | 8,799.547 ns | 482.333 ns | 16800 B |
| Mockolate | 11,074.26 ns | 9,154.210 ns | 501.773 ns | 8400 B |
| Moq | 79,608.76 ns | 7,940.504 ns | 435.246 ns | 37600 B |
| NSubstitute | 72,743.88 ns | 15,404.146 ns | 844.353 ns | 30848 B |
| FakeItEasy | 175,982.51 ns | 65,881.447 ns | 3,611.184 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 211180
  bar [26067.49, 28722.53, 11074.26, 79608.76, 72743.88, 175982.51]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-21T03:28:27.059Z*
