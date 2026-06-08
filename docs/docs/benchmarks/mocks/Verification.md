---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 569.74 ns | 0.570 ns | 0.476 ns | 3000 B |
| Imposter | 533.41 ns | 2.553 ns | 2.132 ns | 4688 B |
| Mockolate | 305.76 ns | 0.564 ns | 0.440 ns | 2240 B |
| Moq | 191,047.44 ns | 821.988 ns | 728.671 ns | 24336 B |
| NSubstitute | 4,406.61 ns | 9.835 ns | 8.213 ns | 10064 B |
| FakeItEasy | 4,986.45 ns | 19.981 ns | 17.712 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 229257
  bar [569.74, 533.41, 305.76, 191047.44, 4406.61, 4986.45]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 44.65 ns | 0.540 ns | 0.506 ns | 312 B |
| Imposter | 245.21 ns | 1.106 ns | 1.034 ns | 2400 B |
| Mockolate | 188.66 ns | 0.760 ns | 0.711 ns | 1240 B |
| Moq | 49,673.29 ns | 239.392 ns | 223.927 ns | 7149 B |
| NSubstitute | 2,605.87 ns | 9.264 ns | 7.736 ns | 7088 B |
| FakeItEasy | 2,434.96 ns | 10.126 ns | 8.455 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 59608
  bar [44.65, 245.21, 188.66, 49673.29, 2605.87, 2434.96]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 955.82 ns | 9.021 ns | 8.438 ns | 4464 B |
| Imposter | 1,334.92 ns | 5.124 ns | 4.793 ns | 11192 B |
| Mockolate | 886.10 ns | 4.588 ns | 4.292 ns | 5376 B |
| Moq | 271,668.78 ns | 781.853 ns | 693.093 ns | 34699 B |
| NSubstitute | 7,913.22 ns | 36.087 ns | 33.756 ns | 16762 B |
| FakeItEasy | 8,872.98 ns | 61.636 ns | 51.469 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 326003
  bar [955.82, 1334.92, 886.1, 271668.78, 7913.22, 8872.98]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-08T03:30:49.435Z*
