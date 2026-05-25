---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 684.54 ns | 3.110 ns | 2.909 ns | 2968 B |
| Imposter | 682.82 ns | 1.128 ns | 1.000 ns | 4688 B |
| Mockolate | 413.12 ns | 0.641 ns | 0.568 ns | 2240 B |
| Moq | 343,264.62 ns | 1,627.424 ns | 1,442.669 ns | 24325 B |
| NSubstitute | 6,181.42 ns | 18.386 ns | 16.299 ns | 10064 B |
| FakeItEasy | 7,205.94 ns | 14.354 ns | 12.724 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411918
  bar [684.54, 682.82, 413.12, 343264.62, 6181.42, 7205.94]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.25 ns | 0.084 ns | 0.075 ns | 304 B |
| Imposter | 303.43 ns | 2.267 ns | 2.010 ns | 2400 B |
| Mockolate | 225.65 ns | 1.379 ns | 1.222 ns | 1240 B |
| Moq | 87,303.66 ns | 197.628 ns | 175.192 ns | 6918 B |
| NSubstitute | 3,504.29 ns | 6.645 ns | 5.890 ns | 7088 B |
| FakeItEasy | 3,573.10 ns | 10.343 ns | 8.637 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104765
  bar [46.25, 303.43, 225.65, 87303.66, 3504.29, 3573.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,177.96 ns | 2.613 ns | 2.316 ns | 4384 B |
| Imposter | 1,769.47 ns | 13.372 ns | 11.854 ns | 11192 B |
| Mockolate | 1,101.09 ns | 9.054 ns | 8.469 ns | 5376 B |
| Moq | 476,516.26 ns | 2,783.602 ns | 2,603.783 ns | 34699 B |
| NSubstitute | 11,498.09 ns | 77.448 ns | 64.672 ns | 16763 B |
| FakeItEasy | 13,160.70 ns | 116.560 ns | 103.328 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 571820
  bar [1177.96, 1769.47, 1101.09, 476516.26, 11498.09, 13160.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-25T03:29:24.567Z*
