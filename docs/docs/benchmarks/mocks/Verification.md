---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 730.16 ns | 9.955 ns | 9.312 ns | 3008 B |
| Imposter | 700.83 ns | 13.464 ns | 14.406 ns | 4688 B |
| Mockolate | 419.96 ns | 8.002 ns | 8.217 ns | 2128 B |
| Moq | 349,604.63 ns | 2,777.214 ns | 2,597.808 ns | 24325 B |
| NSubstitute | 6,546.07 ns | 39.292 ns | 36.754 ns | 10064 B |
| FakeItEasy | 7,674.36 ns | 60.277 ns | 50.334 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 419526
  bar [730.16, 700.83, 419.96, 349604.63, 6546.07, 7674.36]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.97 ns | 0.935 ns | 0.875 ns | 320 B |
| Imposter | 336.95 ns | 6.572 ns | 8.071 ns | 2400 B |
| Mockolate | 237.05 ns | 4.633 ns | 4.551 ns | 1144 B |
| Moq | 88,995.44 ns | 743.039 ns | 695.040 ns | 6918 B |
| NSubstitute | 3,611.83 ns | 22.236 ns | 20.800 ns | 7088 B |
| FakeItEasy | 3,633.38 ns | 65.816 ns | 58.344 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106795
  bar [54.97, 336.95, 237.05, 88995.44, 3611.83, 3633.38]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,272.50 ns | 20.962 ns | 22.429 ns | 4472 B |
| Imposter | 1,745.40 ns | 32.378 ns | 30.287 ns | 11192 B |
| Mockolate | 1,093.69 ns | 18.791 ns | 17.577 ns | 5240 B |
| Moq | 470,848.86 ns | 2,607.787 ns | 2,439.325 ns | 34946 B |
| NSubstitute | 11,594.33 ns | 103.480 ns | 80.790 ns | 16891 B |
| FakeItEasy | 14,550.44 ns | 284.098 ns | 303.981 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 565019
  bar [1272.5, 1745.4, 1093.69, 470848.86, 11594.33, 14550.44]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-19T03:27:20.624Z*
