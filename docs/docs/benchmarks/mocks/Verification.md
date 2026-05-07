---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 719.38 ns | 12.254 ns | 11.462 ns | 2864 B |
| Imposter | 739.62 ns | 14.038 ns | 24.587 ns | 4688 B |
| Mockolate | 437.41 ns | 6.116 ns | 5.721 ns | 2224 B |
| Moq | 348,573.71 ns | 1,467.544 ns | 1,300.940 ns | 24325 B |
| NSubstitute | 6,556.26 ns | 66.140 ns | 61.867 ns | 10064 B |
| FakeItEasy | 7,439.71 ns | 48.847 ns | 45.692 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 418289
  bar [719.38, 739.62, 437.41, 348573.71, 6556.26, 7439.71]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.80 ns | 1.069 ns | 1.144 ns | 304 B |
| Imposter | 349.18 ns | 4.059 ns | 3.169 ns | 2400 B |
| Mockolate | 272.03 ns | 5.307 ns | 5.679 ns | 1240 B |
| Moq | 88,566.54 ns | 330.274 ns | 275.794 ns | 6918 B |
| NSubstitute | 3,701.24 ns | 47.388 ns | 44.326 ns | 7088 B |
| FakeItEasy | 3,867.28 ns | 39.721 ns | 35.212 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106280
  bar [52.8, 349.18, 272.03, 88566.54, 3701.24, 3867.28]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,244.55 ns | 10.659 ns | 9.970 ns | 4176 B |
| Imposter | 1,882.41 ns | 31.474 ns | 29.441 ns | 11192 B |
| Mockolate | 1,229.93 ns | 21.381 ns | 20.000 ns | 5408 B |
| Moq | 485,545.95 ns | 2,779.339 ns | 2,599.795 ns | 34699 B |
| NSubstitute | 11,567.51 ns | 56.901 ns | 50.442 ns | 16762 B |
| FakeItEasy | 14,031.00 ns | 137.333 ns | 128.462 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 582656
  bar [1244.55, 1882.41, 1229.93, 485545.95, 11567.51, 14031]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-07T03:27:11.074Z*
