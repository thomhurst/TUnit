---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 727.78 ns | 10.528 ns | 9.848 ns | 3040 B |
| Imposter | 267.11 ns | 3.613 ns | 3.203 ns | 1096 B |
| Mockolate | 408.77 ns | 5.952 ns | 5.568 ns | 2104 B |
| Moq | 366,842.93 ns | 2,307.470 ns | 2,158.409 ns | 24300 B |
| NSubstitute | 6,864.33 ns | 27.243 ns | 21.270 ns | 10024 B |
| FakeItEasy | 7,024.86 ns | 32.450 ns | 30.354 ns | 10506 B |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 440212
  bar [727.78, 267.11, 408.77, 366842.93, 6864.33, 7024.86]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.33 ns | 0.771 ns | 0.721 ns | 336 B |
| Imposter | 122.68 ns | 0.946 ns | 0.885 ns | 616 B |
| Mockolate | 212.60 ns | 1.835 ns | 1.532 ns | 1104 B |
| Moq | 96,915.99 ns | 1,390.515 ns | 1,300.689 ns | 7029 B |
| NSubstitute | 3,963.07 ns | 32.762 ns | 30.645 ns | 7056 B |
| FakeItEasy | 3,672.91 ns | 17.249 ns | 15.291 ns | 5105 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 116300
  bar [49.33, 122.68, 212.6, 96915.99, 3963.07, 3672.91]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,407.52 ns | 18.576 ns | 16.467 ns | 4528 B |
| Imposter | 608.32 ns | 11.782 ns | 11.572 ns | 2288 B |
| Mockolate | 1,235.63 ns | 10.513 ns | 9.319 ns | 5160 B |
| Moq | 515,043.60 ns | 2,660.843 ns | 2,358.768 ns | 34890 B |
| NSubstitute | 12,863.93 ns | 54.707 ns | 48.496 ns | 16633 B |
| FakeItEasy | 13,277.87 ns | 108.484 ns | 101.476 ns | 19040 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 618053
  bar [1407.52, 608.32, 1235.63, 515043.6, 12863.93, 13277.87]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-16T02:25:25.447Z*
