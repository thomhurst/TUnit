---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 752.08 ns | 14.565 ns | 14.305 ns | 3008 B |
| Imposter | 733.43 ns | 8.682 ns | 7.250 ns | 4688 B |
| Mockolate | 425.97 ns | 8.296 ns | 10.491 ns | 2128 B |
| Moq | 343,393.64 ns | 1,660.046 ns | 1,296.055 ns | 24325 B |
| NSubstitute | 7,194.98 ns | 70.799 ns | 66.226 ns | 10064 B |
| FakeItEasy | 8,201.11 ns | 74.885 ns | 70.048 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 412073
  bar [752.08, 733.43, 425.97, 343393.64, 7194.98, 8201.11]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.45 ns | 0.586 ns | 0.519 ns | 320 B |
| Imposter | 391.30 ns | 6.788 ns | 6.350 ns | 2400 B |
| Mockolate | 238.81 ns | 4.824 ns | 12.875 ns | 1144 B |
| Moq | 87,318.61 ns | 394.964 ns | 329.813 ns | 6918 B |
| NSubstitute | 3,789.74 ns | 17.798 ns | 15.777 ns | 7088 B |
| FakeItEasy | 3,583.02 ns | 31.450 ns | 27.880 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 104783
  bar [60.45, 391.3, 238.81, 87318.61, 3789.74, 3583.02]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,241.51 ns | 12.647 ns | 11.211 ns | 4472 B |
| Imposter | 1,733.90 ns | 12.218 ns | 11.429 ns | 11192 B |
| Mockolate | 1,080.70 ns | 10.710 ns | 10.018 ns | 5240 B |
| Moq | 479,335.15 ns | 1,899.719 ns | 1,776.999 ns | 35130 B |
| NSubstitute | 12,201.92 ns | 36.627 ns | 30.586 ns | 16762 B |
| FakeItEasy | 13,539.02 ns | 181.343 ns | 169.629 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 575203
  bar [1241.51, 1733.9, 1080.7, 479335.15, 12201.92, 13539.02]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-28T05:02:48.374Z*
