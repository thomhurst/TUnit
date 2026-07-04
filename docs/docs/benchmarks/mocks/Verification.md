---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 703.59 ns | 6.403 ns | 5.990 ns | 3008 B |
| Imposter | 668.67 ns | 3.259 ns | 3.048 ns | 4688 B |
| Mockolate | 390.96 ns | 1.575 ns | 1.473 ns | 2128 B |
| Moq | 347,722.00 ns | 2,247.383 ns | 2,102.204 ns | 24325 B |
| NSubstitute | 6,393.07 ns | 40.953 ns | 34.198 ns | 10064 B |
| FakeItEasy | 7,621.42 ns | 41.747 ns | 39.050 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417267
  bar [703.59, 668.67, 390.96, 347722, 6393.07, 7621.42]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.48 ns | 0.150 ns | 0.133 ns | 320 B |
| Imposter | 308.59 ns | 2.692 ns | 2.518 ns | 2400 B |
| Mockolate | 228.15 ns | 0.633 ns | 0.562 ns | 1144 B |
| Moq | 88,724.25 ns | 322.796 ns | 301.943 ns | 6918 B |
| NSubstitute | 3,657.47 ns | 58.199 ns | 51.592 ns | 7088 B |
| FakeItEasy | 3,551.15 ns | 51.586 ns | 45.730 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106470
  bar [50.48, 308.59, 228.15, 88724.25, 3657.47, 3551.15]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,223.85 ns | 4.614 ns | 4.090 ns | 4472 B |
| Imposter | 1,703.16 ns | 33.618 ns | 40.020 ns | 11192 B |
| Mockolate | 1,073.57 ns | 4.084 ns | 3.620 ns | 5240 B |
| Moq | 474,485.71 ns | 2,540.546 ns | 2,376.428 ns | 34811 B |
| NSubstitute | 11,225.44 ns | 36.795 ns | 32.618 ns | 16929 B |
| FakeItEasy | 13,351.86 ns | 154.209 ns | 136.702 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569383
  bar [1223.85, 1703.16, 1073.57, 474485.71, 11225.44, 13351.86]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-04T03:22:20.303Z*
