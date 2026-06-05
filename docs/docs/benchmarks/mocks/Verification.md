---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.28 ns | 14.638 ns | 29.233 ns | 3000 B |
| Imposter | 735.96 ns | 12.898 ns | 11.434 ns | 4688 B |
| Mockolate | 427.34 ns | 7.245 ns | 6.777 ns | 2240 B |
| Moq | 373,996.51 ns | 3,345.434 ns | 3,129.322 ns | 24325 B |
| NSubstitute | 6,486.31 ns | 98.951 ns | 87.717 ns | 10064 B |
| FakeItEasy | 8,142.20 ns | 96.229 ns | 90.012 ns | 10884 B |

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
  y-axis "Time (ns)" 0 --> 448796
  bar [740.28, 735.96, 427.34, 373996.51, 6486.31, 8142.2]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.07 ns | 0.548 ns | 0.513 ns | 312 B |
| Imposter | 334.54 ns | 6.560 ns | 15.717 ns | 2400 B |
| Mockolate | 256.35 ns | 2.250 ns | 1.995 ns | 1240 B |
| Moq | 89,675.47 ns | 826.207 ns | 772.835 ns | 6918 B |
| NSubstitute | 3,894.99 ns | 34.632 ns | 32.395 ns | 7088 B |
| FakeItEasy | 3,914.64 ns | 27.648 ns | 25.862 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107611
  bar [51.07, 334.54, 256.35, 89675.47, 3894.99, 3914.64]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,386.41 ns | 26.113 ns | 26.816 ns | 4464 B |
| Imposter | 1,920.83 ns | 37.481 ns | 59.448 ns | 11192 B |
| Mockolate | 1,295.65 ns | 25.490 ns | 25.035 ns | 5376 B |
| Moq | 490,495.54 ns | 4,630.840 ns | 4,331.691 ns | 34699 B |
| NSubstitute | 12,018.59 ns | 163.074 ns | 152.540 ns | 16763 B |
| FakeItEasy | 14,170.14 ns | 140.156 ns | 124.245 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 588595
  bar [1386.41, 1920.83, 1295.65, 490495.54, 12018.59, 14170.14]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-05T03:30:04.148Z*
