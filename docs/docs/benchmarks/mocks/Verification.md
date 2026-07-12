---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 834.72 ns | 8.448 ns | 7.489 ns | 3008 B |
| Imposter | 766.90 ns | 15.127 ns | 14.149 ns | 4688 B |
| Mockolate | 478.83 ns | 9.443 ns | 10.496 ns | 2128 B |
| Moq | 256,466.16 ns | 956.068 ns | 847.529 ns | 24306 B |
| NSubstitute | 6,364.72 ns | 22.520 ns | 19.964 ns | 10064 B |
| FakeItEasy | 6,788.61 ns | 33.357 ns | 31.202 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 307760
  bar [834.72, 766.9, 478.83, 256466.16, 6364.72, 6788.61]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.63 ns | 1.108 ns | 1.479 ns | 320 B |
| Imposter | 343.11 ns | 3.799 ns | 3.172 ns | 2400 B |
| Mockolate | 259.00 ns | 5.221 ns | 4.628 ns | 1144 B |
| Moq | 65,295.94 ns | 189.919 ns | 158.591 ns | 6925 B |
| NSubstitute | 3,326.49 ns | 11.176 ns | 10.454 ns | 7088 B |
| FakeItEasy | 3,529.32 ns | 48.379 ns | 42.887 ns | 5217 B |

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
  y-axis "Time (ns)" 0 --> 78356
  bar [54.63, 343.11, 259, 65295.94, 3326.49, 3529.32]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,410.00 ns | 16.509 ns | 15.442 ns | 4472 B |
| Imposter | 1,866.04 ns | 25.378 ns | 23.739 ns | 11192 B |
| Mockolate | 1,230.43 ns | 24.367 ns | 27.084 ns | 5240 B |
| Moq | 351,884.55 ns | 2,212.835 ns | 1,847.818 ns | 34670 B |
| NSubstitute | 11,315.86 ns | 57.979 ns | 54.234 ns | 16762 B |
| FakeItEasy | 12,311.93 ns | 43.549 ns | 38.605 ns | 19239 B |

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
  y-axis "Time (ns)" 0 --> 422262
  bar [1410, 1866.04, 1230.43, 351884.55, 11315.86, 12311.93]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-12T03:30:57.252Z*
