---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 795.79 ns | 8.104 ns | 7.184 ns | 2864 B |
| Imposter | 828.20 ns | 15.411 ns | 15.136 ns | 4688 B |
| Mockolate | 1,051.88 ns | 7.068 ns | 5.902 ns | 3152 B |
| Moq | 253,160.05 ns | 1,275.335 ns | 1,130.551 ns | 24306 B |
| NSubstitute | 6,476.44 ns | 25.473 ns | 23.828 ns | 10064 B |
| FakeItEasy | 6,997.74 ns | 27.450 ns | 25.677 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 303793
  bar [795.79, 828.2, 1051.88, 253160.05, 6476.44, 6997.74]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.10 ns | 1.117 ns | 1.045 ns | 304 B |
| Imposter | 392.33 ns | 7.318 ns | 6.845 ns | 2400 B |
| Mockolate | 249.78 ns | 3.384 ns | 3.165 ns | 952 B |
| Moq | 65,416.34 ns | 265.102 ns | 247.977 ns | 6925 B |
| NSubstitute | 3,591.72 ns | 11.007 ns | 10.296 ns | 7088 B |
| FakeItEasy | 3,555.21 ns | 21.558 ns | 18.002 ns | 5217 B |

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
  y-axis "Time (ns)" 0 --> 78500
  bar [57.1, 392.33, 249.78, 65416.34, 3591.72, 3555.21]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,337.80 ns | 15.412 ns | 13.663 ns | 4176 B |
| Imposter | 1,984.41 ns | 38.846 ns | 47.706 ns | 11192 B |
| Mockolate | 2,015.36 ns | 10.147 ns | 8.473 ns | 5496 B |
| Moq | 359,994.11 ns | 2,231.418 ns | 1,863.335 ns | 34782 B |
| NSubstitute | 11,325.34 ns | 30.534 ns | 28.561 ns | 16890 B |
| FakeItEasy | 12,353.64 ns | 90.569 ns | 80.287 ns | 19238 B |

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
  y-axis "Time (ns)" 0 --> 431993
  bar [1337.8, 1984.41, 2015.36, 359994.11, 11325.34, 12353.64]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-25T03:21:02.718Z*
