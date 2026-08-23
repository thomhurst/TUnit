---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 793.90 ns | 8.650 ns | 7.668 ns | 3008 B |
| Imposter | 720.02 ns | 10.758 ns | 10.063 ns | 4688 B |
| Mockolate | 418.74 ns | 3.013 ns | 2.818 ns | 2128 B |
| Moq | 243,230.07 ns | 1,540.671 ns | 1,365.764 ns | 24324 B |
| NSubstitute | 6,812.11 ns | 70.816 ns | 62.777 ns | 10064 B |
| FakeItEasy | 6,884.14 ns | 65.277 ns | 61.060 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 291877
  bar [793.9, 720.02, 418.74, 243230.07, 6812.11, 6884.14]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.73 ns | 0.334 ns | 0.312 ns | 320 B |
| Imposter | 359.60 ns | 5.328 ns | 4.984 ns | 2400 B |
| Mockolate | 263.44 ns | 2.979 ns | 2.787 ns | 1144 B |
| Moq | 62,401.04 ns | 343.809 ns | 287.096 ns | 6925 B |
| NSubstitute | 3,933.09 ns | 49.636 ns | 46.429 ns | 7088 B |
| FakeItEasy | 3,442.92 ns | 68.267 ns | 60.517 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74882
  bar [57.73, 359.6, 263.44, 62401.04, 3933.09, 3442.92]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,307.20 ns | 8.444 ns | 7.486 ns | 4472 B |
| Imposter | 1,937.76 ns | 22.137 ns | 20.707 ns | 11192 B |
| Mockolate | 1,289.65 ns | 21.337 ns | 19.958 ns | 5240 B |
| Moq | 353,404.41 ns | 2,394.634 ns | 1,999.628 ns | 34779 B |
| NSubstitute | 11,651.25 ns | 79.612 ns | 70.574 ns | 16762 B |
| FakeItEasy | 12,105.22 ns | 169.758 ns | 158.791 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 424086
  bar [1307.2, 1937.76, 1289.65, 353404.41, 11651.25, 12105.22]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-23T02:45:27.613Z*
