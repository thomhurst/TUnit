---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.63 ns | 11.651 ns | 10.898 ns | 3008 B |
| Imposter | 692.40 ns | 4.085 ns | 3.189 ns | 4688 B |
| Mockolate | 665.27 ns | 4.811 ns | 4.265 ns | 2240 B |
| Moq | 352,845.11 ns | 1,603.079 ns | 1,421.088 ns | 24325 B |
| NSubstitute | 6,140.21 ns | 51.436 ns | 42.952 ns | 10064 B |
| FakeItEasy | 7,530.96 ns | 36.379 ns | 30.378 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 423415
  bar [755.63, 692.4, 665.27, 352845.11, 6140.21, 7530.96]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.42 ns | 1.095 ns | 2.426 ns | 320 B |
| Imposter | 321.12 ns | 6.479 ns | 12.788 ns | 2400 B |
| Mockolate | 252.38 ns | 2.109 ns | 1.870 ns | 1240 B |
| Moq | 91,249.32 ns | 425.058 ns | 354.943 ns | 6918 B |
| NSubstitute | 3,697.83 ns | 29.645 ns | 26.280 ns | 7088 B |
| FakeItEasy | 3,696.76 ns | 71.580 ns | 79.561 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 109500
  bar [53.42, 321.12, 252.38, 91249.32, 3697.83, 3696.76]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,283.81 ns | 19.607 ns | 17.381 ns | 4472 B |
| Imposter | 1,863.36 ns | 36.756 ns | 75.907 ns | 11192 B |
| Mockolate | 1,267.61 ns | 25.332 ns | 26.014 ns | 5376 B |
| Moq | 488,528.61 ns | 2,205.250 ns | 1,841.483 ns | 34842 B |
| NSubstitute | 11,849.90 ns | 148.362 ns | 131.519 ns | 16891 B |
| FakeItEasy | 14,546.37 ns | 275.970 ns | 328.523 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 586235
  bar [1283.81, 1863.36, 1267.61, 488528.61, 11849.9, 14546.37]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-21T03:36:43.702Z*
