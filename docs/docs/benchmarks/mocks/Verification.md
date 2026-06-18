---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 788.12 ns | 3.045 ns | 2.543 ns | 3008 B |
| Imposter | 739.02 ns | 6.395 ns | 5.982 ns | 4688 B |
| Mockolate | 432.70 ns | 2.305 ns | 2.043 ns | 2240 B |
| Moq | 252,690.26 ns | 1,355.022 ns | 1,201.191 ns | 24324 B |
| NSubstitute | 6,171.27 ns | 42.226 ns | 37.432 ns | 10064 B |
| FakeItEasy | 6,787.33 ns | 59.435 ns | 52.687 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 303229
  bar [788.12, 739.02, 432.7, 252690.26, 6171.27, 6787.33]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.55 ns | 0.167 ns | 0.139 ns | 320 B |
| Imposter | 339.76 ns | 5.309 ns | 4.966 ns | 2400 B |
| Mockolate | 257.21 ns | 2.277 ns | 2.130 ns | 1240 B |
| Moq | 64,156.37 ns | 377.022 ns | 314.830 ns | 7005 B |
| NSubstitute | 3,591.51 ns | 45.024 ns | 42.116 ns | 7088 B |
| FakeItEasy | 3,466.47 ns | 60.666 ns | 53.779 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 76988
  bar [56.55, 339.76, 257.21, 64156.37, 3591.51, 3466.47]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,342.57 ns | 23.123 ns | 21.629 ns | 4472 B |
| Imposter | 1,800.07 ns | 27.614 ns | 25.830 ns | 11192 B |
| Mockolate | 1,191.64 ns | 22.811 ns | 24.408 ns | 5376 B |
| Moq | 363,074.92 ns | 3,154.952 ns | 2,951.144 ns | 34922 B |
| NSubstitute | 10,887.68 ns | 83.524 ns | 78.128 ns | 16762 B |
| FakeItEasy | 12,157.60 ns | 144.392 ns | 135.065 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 435690
  bar [1342.57, 1800.07, 1191.64, 363074.92, 10887.68, 12157.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-18T03:29:53.480Z*
