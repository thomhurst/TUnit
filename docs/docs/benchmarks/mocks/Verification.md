---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 711.23 ns | 11.135 ns | 9.298 ns | 2864 B |
| Imposter | 678.66 ns | 13.360 ns | 18.729 ns | 4688 B |
| Mockolate | 955.18 ns | 18.816 ns | 20.914 ns | 3152 B |
| Moq | 247,269.31 ns | 1,888.028 ns | 1,576.589 ns | 24578 B |
| NSubstitute | 5,910.96 ns | 53.713 ns | 50.243 ns | 10064 B |
| FakeItEasy | 6,690.39 ns | 100.000 ns | 93.540 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 296724
  bar [711.23, 678.66, 955.18, 247269.31, 5910.96, 6690.39]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.98 ns | 0.327 ns | 0.290 ns | 304 B |
| Imposter | 349.10 ns | 4.902 ns | 4.586 ns | 2400 B |
| Mockolate | 237.34 ns | 3.649 ns | 3.235 ns | 952 B |
| Moq | 61,111.81 ns | 584.431 ns | 546.677 ns | 6925 B |
| NSubstitute | 3,468.99 ns | 25.667 ns | 22.753 ns | 7088 B |
| FakeItEasy | 3,324.76 ns | 30.854 ns | 28.861 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 73335
  bar [53.98, 349.1, 237.34, 61111.81, 3468.99, 3324.76]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,177.01 ns | 9.208 ns | 7.689 ns | 4176 B |
| Imposter | 1,772.97 ns | 35.398 ns | 89.455 ns | 11192 B |
| Mockolate | 1,920.12 ns | 37.836 ns | 49.197 ns | 5496 B |
| Moq | 350,702.23 ns | 2,193.130 ns | 2,051.455 ns | 34699 B |
| NSubstitute | 10,626.33 ns | 121.547 ns | 107.748 ns | 16762 B |
| FakeItEasy | 11,730.48 ns | 182.562 ns | 161.836 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 420843
  bar [1177.01, 1772.97, 1920.12, 350702.23, 10626.33, 11730.48]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-26T03:29:14.435Z*
