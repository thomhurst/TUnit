---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 742.22 ns | 6.100 ns | 5.706 ns | 2968 B |
| Imposter | 730.49 ns | 7.966 ns | 7.451 ns | 4688 B |
| Mockolate | 459.63 ns | 6.461 ns | 5.728 ns | 2240 B |
| Moq | 354,119.00 ns | 2,642.356 ns | 2,342.380 ns | 24325 B |
| NSubstitute | 6,559.21 ns | 56.931 ns | 50.468 ns | 10064 B |
| FakeItEasy | 7,645.30 ns | 34.864 ns | 30.906 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 424943
  bar [742.22, 730.49, 459.63, 354119, 6559.21, 7645.3]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.25 ns | 0.557 ns | 0.494 ns | 304 B |
| Imposter | 357.21 ns | 7.127 ns | 7.319 ns | 2400 B |
| Mockolate | 263.61 ns | 3.991 ns | 3.733 ns | 1240 B |
| Moq | 90,354.61 ns | 403.815 ns | 337.204 ns | 6918 B |
| NSubstitute | 3,723.91 ns | 45.534 ns | 40.365 ns | 7088 B |
| FakeItEasy | 3,799.59 ns | 37.420 ns | 31.248 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 108426
  bar [50.25, 357.21, 263.61, 90354.61, 3723.91, 3799.59]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,280.85 ns | 8.998 ns | 7.976 ns | 4384 B |
| Imposter | 1,893.39 ns | 36.935 ns | 49.307 ns | 11192 B |
| Mockolate | 1,210.28 ns | 20.724 ns | 19.386 ns | 5376 B |
| Moq | 487,172.38 ns | 1,871.694 ns | 1,562.949 ns | 34699 B |
| NSubstitute | 11,676.69 ns | 72.368 ns | 67.693 ns | 16763 B |
| FakeItEasy | 14,260.43 ns | 240.726 ns | 213.397 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 584607
  bar [1280.85, 1893.39, 1210.28, 487172.38, 11676.69, 14260.43]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-30T03:25:40.021Z*
