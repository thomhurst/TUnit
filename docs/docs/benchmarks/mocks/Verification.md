---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 905.98 ns | 6.020 ns | 5.631 ns | 3024 B |
| Imposter | 939.87 ns | 8.148 ns | 7.223 ns | 4664 B |
| Mockolate | 558.25 ns | 3.851 ns | 3.602 ns | 2128 B |
| Moq | 171,921.64 ns | 1,155.964 ns | 1,024.732 ns | 24482 B |
| NSubstitute | 7,053.30 ns | 36.413 ns | 34.061 ns | 10064 B |
| FakeItEasy | 6,073.60 ns | 48.172 ns | 42.703 ns | 10719 B |

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
  y-axis "Time (ns)" 0 --> 206306
  bar [905.98, 939.87, 558.25, 171921.64, 7053.3, 6073.6]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 69.30 ns | 0.224 ns | 0.210 ns | 320 B |
| Imposter | 442.48 ns | 8.161 ns | 7.633 ns | 2384 B |
| Mockolate | 290.60 ns | 2.356 ns | 2.204 ns | 1144 B |
| Moq | 43,709.12 ns | 163.796 ns | 145.201 ns | 6904 B |
| NSubstitute | 3,675.08 ns | 18.374 ns | 15.343 ns | 7088 B |
| FakeItEasy | 2,892.67 ns | 20.206 ns | 17.912 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 52451
  bar [69.3, 442.48, 290.6, 43709.12, 3675.08, 2892.67]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,470.46 ns | 4.227 ns | 3.747 ns | 4512 B |
| Imposter | 2,144.15 ns | 7.493 ns | 6.642 ns | 11144 B |
| Mockolate | 1,335.75 ns | 15.121 ns | 14.144 ns | 5240 B |
| Moq | 225,203.44 ns | 902.248 ns | 843.963 ns | 34584 B |
| NSubstitute | 12,585.10 ns | 66.886 ns | 59.293 ns | 16760 B |
| FakeItEasy | 10,697.18 ns | 116.383 ns | 97.185 ns | 19238 B |

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
  y-axis "Time (ns)" 0 --> 270245
  bar [1470.46, 2144.15, 1335.75, 225203.44, 12585.1, 10697.18]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-09T02:42:26.464Z*
