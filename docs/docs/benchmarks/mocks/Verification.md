---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 739.22 ns | 4.574 ns | 4.054 ns | 3008 B |
| Imposter | 706.45 ns | 9.734 ns | 8.128 ns | 4688 B |
| Mockolate | 406.95 ns | 5.133 ns | 4.550 ns | 2128 B |
| Moq | 349,984.31 ns | 1,671.847 ns | 1,482.049 ns | 24325 B |
| NSubstitute | 7,034.08 ns | 101.296 ns | 89.796 ns | 10064 B |
| FakeItEasy | 7,368.20 ns | 33.610 ns | 29.795 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 419982
  bar [739.22, 706.45, 406.95, 349984.31, 7034.08, 7368.2]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.01 ns | 0.784 ns | 0.733 ns | 320 B |
| Imposter | 329.03 ns | 3.990 ns | 3.733 ns | 2400 B |
| Mockolate | 226.83 ns | 3.376 ns | 3.158 ns | 1144 B |
| Moq | 89,036.49 ns | 607.797 ns | 507.538 ns | 7030 B |
| NSubstitute | 3,898.84 ns | 24.305 ns | 20.296 ns | 7088 B |
| FakeItEasy | 3,545.79 ns | 62.897 ns | 55.757 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106844
  bar [52.01, 329.03, 226.83, 89036.49, 3898.84, 3545.79]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,272.48 ns | 9.277 ns | 8.677 ns | 4472 B |
| Imposter | 1,770.07 ns | 26.008 ns | 24.328 ns | 11192 B |
| Mockolate | 1,119.32 ns | 12.401 ns | 10.993 ns | 5240 B |
| Moq | 464,767.97 ns | 1,606.418 ns | 1,254.186 ns | 34699 B |
| NSubstitute | 12,396.51 ns | 37.179 ns | 31.046 ns | 16762 B |
| FakeItEasy | 13,902.93 ns | 180.638 ns | 160.131 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 557722
  bar [1272.48, 1770.07, 1119.32, 464767.97, 12396.51, 13902.93]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-25T02:32:28.119Z*
