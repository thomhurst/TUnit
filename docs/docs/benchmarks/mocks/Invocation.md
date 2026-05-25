---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 259.66 ns | 58.23 ns | 3.192 ns | 120 B |
| Imposter | 288.73 ns | 75.17 ns | 4.120 ns | 168 B |
| Mockolate | 100.04 ns | 26.21 ns | 1.437 ns | 84 B |
| Moq | 784.36 ns | 206.02 ns | 11.292 ns | 376 B |
| NSubstitute | 691.11 ns | 64.82 ns | 3.553 ns | 304 B |
| FakeItEasy | 1,678.74 ns | 668.83 ns | 36.661 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2015
  bar [259.66, 288.73, 100.04, 784.36, 691.11, 1678.74]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 158.77 ns | 72.90 ns | 3.996 ns | 88 B |
| Imposter | 287.83 ns | 56.81 ns | 3.114 ns | 168 B |
| Mockolate | 90.76 ns | 29.79 ns | 1.633 ns | 60 B |
| Moq | 514.18 ns | 82.00 ns | 4.495 ns | 296 B |
| NSubstitute | 604.24 ns | 336.93 ns | 18.468 ns | 272 B |
| FakeItEasy | 1,487.68 ns | 428.57 ns | 23.492 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1786
  bar [158.77, 287.83, 90.76, 514.18, 604.24, 1487.68]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,914.65 ns | 9,855.30 ns | 540.202 ns | 11936 B |
| Imposter | 28,381.28 ns | 8,355.31 ns | 457.982 ns | 16800 B |
| Mockolate | 10,029.83 ns | 6,466.90 ns | 354.472 ns | 8400 B |
| Moq | 78,131.06 ns | 9,271.83 ns | 508.220 ns | 37600 B |
| NSubstitute | 70,476.07 ns | 21,812.86 ns | 1,195.636 ns | 30848 B |
| FakeItEasy | 167,158.20 ns | 44,448.50 ns | 2,436.372 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 200590
  bar [25914.65, 28381.28, 10029.83, 78131.06, 70476.07, 167158.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-25T03:29:24.567Z*
