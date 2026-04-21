---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 881.31 ns | 9.506 ns | 8.892 ns | 3080 B |
| Imposter | 812.30 ns | 16.120 ns | 19.189 ns | 4688 B |
| Mockolate | 1,030.82 ns | 6.977 ns | 6.526 ns | 3152 B |
| Moq | 255,139.15 ns | 1,568.742 ns | 1,467.402 ns | 24306 B |
| NSubstitute | 6,291.22 ns | 16.921 ns | 14.130 ns | 10064 B |
| FakeItEasy | 7,176.87 ns | 27.700 ns | 25.910 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 306167
  bar [881.31, 812.3, 1030.82, 255139.15, 6291.22, 7176.87]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.27 ns | 1.039 ns | 0.921 ns | 328 B |
| Imposter | 372.00 ns | 7.334 ns | 10.281 ns | 2400 B |
| Mockolate | 242.41 ns | 2.774 ns | 2.594 ns | 952 B |
| Moq | 64,732.95 ns | 353.212 ns | 313.114 ns | 6925 B |
| NSubstitute | 3,604.65 ns | 15.819 ns | 14.024 ns | 7088 B |
| FakeItEasy | 3,473.55 ns | 21.592 ns | 20.197 ns | 5218 B |

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
  y-axis "Time (ns)" 0 --> 77680
  bar [60.27, 372, 242.41, 64732.95, 3604.65, 3473.55]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,473.82 ns | 12.775 ns | 11.950 ns | 4608 B |
| Imposter | 1,851.19 ns | 28.484 ns | 26.644 ns | 11192 B |
| Mockolate | 2,013.09 ns | 17.122 ns | 16.016 ns | 5496 B |
| Moq | 349,473.92 ns | 1,519.114 ns | 1,346.655 ns | 34670 B |
| NSubstitute | 10,913.23 ns | 34.866 ns | 30.908 ns | 16762 B |
| FakeItEasy | 12,223.21 ns | 33.359 ns | 27.856 ns | 19239 B |

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
  y-axis "Time (ns)" 0 --> 419369
  bar [1473.82, 1851.19, 2013.09, 349473.92, 10913.23, 12223.21]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-21T03:22:48.421Z*
