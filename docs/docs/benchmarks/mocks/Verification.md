---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 783.75 ns | 3.651 ns | 3.237 ns | 3080 B |
| Imposter | 705.73 ns | 9.531 ns | 8.915 ns | 4688 B |
| Mockolate | 946.62 ns | 9.986 ns | 9.341 ns | 3104 B |
| Moq | 247,261.40 ns | 992.699 ns | 880.002 ns | 24324 B |
| NSubstitute | 5,765.54 ns | 28.496 ns | 25.261 ns | 10064 B |
| FakeItEasy | 6,349.67 ns | 65.960 ns | 58.472 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 296714
  bar [783.75, 705.73, 946.62, 247261.4, 5765.54, 6349.67]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 84.51 ns | 0.464 ns | 0.434 ns | 384 B |
| Imposter | 316.91 ns | 1.919 ns | 1.701 ns | 2400 B |
| Mockolate | 231.30 ns | 3.072 ns | 2.874 ns | 904 B |
| Moq | 62,498.70 ns | 362.639 ns | 339.213 ns | 6925 B |
| NSubstitute | 3,456.72 ns | 65.093 ns | 66.845 ns | 7088 B |
| FakeItEasy | 3,187.45 ns | 14.879 ns | 13.918 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74999
  bar [84.51, 316.91, 231.3, 62498.7, 3456.72, 3187.45]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,326.60 ns | 14.734 ns | 13.783 ns | 4544 B |
| Imposter | 1,693.49 ns | 26.224 ns | 23.247 ns | 11192 B |
| Mockolate | 1,862.24 ns | 8.315 ns | 7.777 ns | 5400 B |
| Moq | 351,617.43 ns | 1,830.903 ns | 1,528.887 ns | 34811 B |
| NSubstitute | 10,706.11 ns | 52.238 ns | 48.864 ns | 16762 B |
| FakeItEasy | 11,468.00 ns | 130.548 ns | 122.114 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 421941
  bar [1326.6, 1693.49, 1862.24, 351617.43, 10706.11, 11468]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-01T03:22:34.139Z*
