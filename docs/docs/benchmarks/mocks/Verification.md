---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 738.04 ns | 11.196 ns | 10.473 ns | 3008 B |
| Imposter | 717.07 ns | 14.294 ns | 20.500 ns | 4688 B |
| Mockolate | 420.57 ns | 8.227 ns | 9.474 ns | 2128 B |
| Moq | 350,102.94 ns | 2,441.819 ns | 2,164.609 ns | 24325 B |
| NSubstitute | 6,313.52 ns | 44.351 ns | 37.035 ns | 10064 B |
| FakeItEasy | 7,682.74 ns | 145.913 ns | 143.306 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 420124
  bar [738.04, 717.07, 420.57, 350102.94, 6313.52, 7682.74]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.35 ns | 0.804 ns | 0.752 ns | 320 B |
| Imposter | 335.76 ns | 6.647 ns | 13.428 ns | 2400 B |
| Mockolate | 244.76 ns | 4.720 ns | 9.853 ns | 1144 B |
| Moq | 89,018.53 ns | 1,035.498 ns | 917.942 ns | 6918 B |
| NSubstitute | 3,700.21 ns | 52.234 ns | 48.860 ns | 7088 B |
| FakeItEasy | 3,813.02 ns | 38.008 ns | 31.738 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106823
  bar [54.35, 335.76, 244.76, 89018.53, 3700.21, 3813.02]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,276.98 ns | 7.024 ns | 5.484 ns | 4472 B |
| Imposter | 1,827.56 ns | 24.932 ns | 23.321 ns | 11192 B |
| Mockolate | 1,148.45 ns | 17.949 ns | 19.205 ns | 5240 B |
| Moq | 486,267.31 ns | 3,754.651 ns | 3,512.103 ns | 34699 B |
| NSubstitute | 11,522.53 ns | 82.264 ns | 72.925 ns | 16762 B |
| FakeItEasy | 13,638.20 ns | 178.156 ns | 166.647 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 583521
  bar [1276.98, 1827.56, 1148.45, 486267.31, 11522.53, 13638.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-21T03:22:31.280Z*
