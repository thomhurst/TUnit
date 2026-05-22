---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 717.66 ns | 3.235 ns | 2.868 ns | 2968 B |
| Imposter | 722.04 ns | 9.941 ns | 8.812 ns | 4688 B |
| Mockolate | 431.79 ns | 5.452 ns | 4.833 ns | 2240 B |
| Moq | 342,648.49 ns | 1,698.196 ns | 1,418.071 ns | 24325 B |
| NSubstitute | 6,577.53 ns | 28.092 ns | 26.277 ns | 10176 B |
| FakeItEasy | 7,780.58 ns | 60.448 ns | 56.543 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411179
  bar [717.66, 722.04, 431.79, 342648.49, 6577.53, 7780.58]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.22 ns | 0.486 ns | 0.455 ns | 304 B |
| Imposter | 338.66 ns | 4.196 ns | 3.925 ns | 2400 B |
| Mockolate | 242.96 ns | 2.758 ns | 2.445 ns | 1240 B |
| Moq | 88,866.10 ns | 210.043 ns | 186.198 ns | 6918 B |
| NSubstitute | 3,773.47 ns | 21.462 ns | 20.075 ns | 7088 B |
| FakeItEasy | 3,811.10 ns | 12.310 ns | 9.611 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106640
  bar [49.22, 338.66, 242.96, 88866.1, 3773.47, 3811.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,384.11 ns | 14.148 ns | 12.542 ns | 4384 B |
| Imposter | 1,813.70 ns | 24.990 ns | 23.376 ns | 11192 B |
| Mockolate | 1,173.12 ns | 9.039 ns | 8.013 ns | 5376 B |
| Moq | 472,383.85 ns | 1,972.690 ns | 1,748.738 ns | 34699 B |
| NSubstitute | 11,672.27 ns | 186.872 ns | 156.046 ns | 16763 B |
| FakeItEasy | 14,663.51 ns | 124.270 ns | 116.242 ns | 19457 B |

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
  y-axis "Time (ns)" 0 --> 566861
  bar [1384.11, 1813.7, 1173.12, 472383.85, 11672.27, 14663.51]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-22T03:28:55.311Z*
