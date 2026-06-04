---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.50 ns | 9.643 ns | 9.020 ns | 3000 B |
| Imposter | 706.63 ns | 9.457 ns | 8.846 ns | 4688 B |
| Mockolate | 398.91 ns | 4.744 ns | 4.205 ns | 2240 B |
| Moq | 246,125.50 ns | 1,141.637 ns | 1,067.888 ns | 24324 B |
| NSubstitute | 5,905.26 ns | 48.267 ns | 42.787 ns | 10064 B |
| FakeItEasy | 6,603.70 ns | 76.271 ns | 67.612 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 295351
  bar [755.5, 706.63, 398.91, 246125.5, 5905.26, 6603.7]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.91 ns | 0.551 ns | 0.515 ns | 312 B |
| Imposter | 332.77 ns | 2.673 ns | 2.370 ns | 2400 B |
| Mockolate | 251.14 ns | 2.223 ns | 2.080 ns | 1240 B |
| Moq | 62,244.11 ns | 402.990 ns | 376.957 ns | 6925 B |
| NSubstitute | 3,600.17 ns | 42.133 ns | 39.412 ns | 7088 B |
| FakeItEasy | 3,294.16 ns | 50.581 ns | 44.838 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74693
  bar [52.91, 332.77, 251.14, 62244.11, 3600.17, 3294.16]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,280.82 ns | 10.134 ns | 9.480 ns | 4464 B |
| Imposter | 1,819.93 ns | 28.043 ns | 24.859 ns | 11192 B |
| Mockolate | 1,143.17 ns | 16.230 ns | 14.387 ns | 5376 B |
| Moq | 357,371.93 ns | 3,390.404 ns | 3,005.505 ns | 34842 B |
| NSubstitute | 10,956.89 ns | 141.411 ns | 132.276 ns | 16763 B |
| FakeItEasy | 11,915.36 ns | 141.659 ns | 132.508 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 428847
  bar [1280.82, 1819.93, 1143.17, 357371.93, 10956.89, 11915.36]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-04T03:31:56.363Z*
