---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 731.54 ns | 4.708 ns | 4.404 ns | 2968 B |
| Imposter | 704.15 ns | 11.142 ns | 10.422 ns | 4688 B |
| Mockolate | 402.06 ns | 3.841 ns | 3.593 ns | 2240 B |
| Moq | 240,476.37 ns | 1,310.722 ns | 1,094.512 ns | 24324 B |
| NSubstitute | 5,855.83 ns | 71.533 ns | 63.412 ns | 10064 B |
| FakeItEasy | 6,368.42 ns | 42.748 ns | 35.697 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 288572
  bar [731.54, 704.15, 402.06, 240476.37, 5855.83, 6368.42]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.52 ns | 0.170 ns | 0.142 ns | 304 B |
| Imposter | 320.22 ns | 3.168 ns | 2.963 ns | 2400 B |
| Mockolate | 254.20 ns | 3.236 ns | 3.027 ns | 1240 B |
| Moq | 62,788.56 ns | 373.250 ns | 311.681 ns | 7037 B |
| NSubstitute | 3,628.13 ns | 26.905 ns | 23.851 ns | 7088 B |
| FakeItEasy | 3,479.17 ns | 37.557 ns | 33.293 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75347
  bar [51.52, 320.22, 254.2, 62788.56, 3628.13, 3479.17]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,287.59 ns | 13.011 ns | 11.534 ns | 4384 B |
| Imposter | 1,783.01 ns | 35.245 ns | 82.383 ns | 11192 B |
| Mockolate | 1,216.74 ns | 23.968 ns | 24.614 ns | 5376 B |
| Moq | 354,879.81 ns | 3,387.848 ns | 3,168.995 ns | 34699 B |
| NSubstitute | 10,539.34 ns | 186.673 ns | 174.614 ns | 16762 B |
| FakeItEasy | 11,423.54 ns | 77.028 ns | 64.322 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 425856
  bar [1287.59, 1783.01, 1216.74, 354879.81, 10539.34, 11423.54]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-26T03:27:58.119Z*
