---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 765.46 ns | 4.664 ns | 4.363 ns | 3048 B |
| Imposter | 678.71 ns | 3.911 ns | 3.658 ns | 4688 B |
| Mockolate | 913.08 ns | 4.103 ns | 3.838 ns | 3152 B |
| Moq | 339,523.10 ns | 890.652 ns | 743.735 ns | 24325 B |
| NSubstitute | 6,175.52 ns | 27.952 ns | 26.147 ns | 10064 B |
| FakeItEasy | 7,269.62 ns | 31.474 ns | 24.573 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 407428
  bar [765.46, 678.71, 913.08, 339523.1, 6175.52, 7269.62]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 79.82 ns | 0.720 ns | 0.674 ns | 376 B |
| Imposter | 311.55 ns | 1.817 ns | 1.611 ns | 2400 B |
| Mockolate | 220.02 ns | 4.440 ns | 5.452 ns | 952 B |
| Moq | 86,094.10 ns | 411.815 ns | 365.063 ns | 6918 B |
| NSubstitute | 3,609.80 ns | 46.432 ns | 43.432 ns | 7088 B |
| FakeItEasy | 3,558.90 ns | 36.803 ns | 32.625 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103313
  bar [79.82, 311.55, 220.02, 86094.1, 3609.8, 3558.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,439.23 ns | 27.793 ns | 30.892 ns | 4544 B |
| Imposter | 1,737.62 ns | 28.705 ns | 28.192 ns | 11192 B |
| Mockolate | 1,813.70 ns | 6.105 ns | 5.412 ns | 5496 B |
| Moq | 469,108.75 ns | 4,201.602 ns | 3,930.181 ns | 34699 B |
| NSubstitute | 11,353.99 ns | 89.215 ns | 83.452 ns | 16763 B |
| FakeItEasy | 14,153.89 ns | 147.468 ns | 130.726 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 562931
  bar [1439.23, 1737.62, 1813.7, 469108.75, 11353.99, 14153.89]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T03:22:20.916Z*
