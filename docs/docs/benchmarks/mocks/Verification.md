---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 719.21 ns | 8.105 ns | 7.185 ns | 3008 B |
| Imposter | 712.97 ns | 10.723 ns | 10.030 ns | 4688 B |
| Mockolate | 415.67 ns | 3.845 ns | 3.596 ns | 2128 B |
| Moq | 351,089.27 ns | 1,514.492 ns | 1,416.656 ns | 24325 B |
| NSubstitute | 6,489.77 ns | 85.869 ns | 80.322 ns | 10064 B |
| FakeItEasy | 7,513.30 ns | 36.524 ns | 30.499 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 421308
  bar [719.21, 712.97, 415.67, 351089.27, 6489.77, 7513.3]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.28 ns | 0.367 ns | 0.325 ns | 320 B |
| Imposter | 332.58 ns | 3.430 ns | 3.208 ns | 2400 B |
| Mockolate | 237.56 ns | 3.279 ns | 2.738 ns | 1144 B |
| Moq | 89,412.68 ns | 318.971 ns | 249.031 ns | 6918 B |
| NSubstitute | 3,694.29 ns | 41.626 ns | 38.937 ns | 7088 B |
| FakeItEasy | 3,750.20 ns | 41.939 ns | 39.230 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107296
  bar [53.28, 332.58, 237.56, 89412.68, 3694.29, 3750.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,288.25 ns | 4.885 ns | 4.570 ns | 4472 B |
| Imposter | 1,816.60 ns | 22.223 ns | 19.701 ns | 11192 B |
| Mockolate | 1,182.63 ns | 19.820 ns | 18.540 ns | 5240 B |
| Moq | 482,019.84 ns | 1,796.626 ns | 1,500.264 ns | 34699 B |
| NSubstitute | 12,071.37 ns | 97.647 ns | 91.339 ns | 16763 B |
| FakeItEasy | 13,541.91 ns | 214.798 ns | 190.413 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 578424
  bar [1288.25, 1816.6, 1182.63, 482019.84, 12071.37, 13541.91]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-25T03:20:44.831Z*
