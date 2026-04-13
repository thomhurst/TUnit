---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 764.54 ns | 4.108 ns | 3.431 ns | 3080 B |
| Imposter | 678.08 ns | 6.564 ns | 5.819 ns | 4688 B |
| Mockolate | 942.96 ns | 15.068 ns | 14.095 ns | 3152 B |
| Moq | 242,640.94 ns | 1,944.850 ns | 1,819.214 ns | 24324 B |
| NSubstitute | 5,870.17 ns | 71.477 ns | 63.363 ns | 10064 B |
| FakeItEasy | 6,493.05 ns | 48.233 ns | 42.757 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 291170
  bar [764.54, 678.08, 942.96, 242640.94, 5870.17, 6493.05]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 69.18 ns | 0.580 ns | 0.543 ns | 328 B |
| Imposter | 319.87 ns | 1.862 ns | 1.742 ns | 2400 B |
| Mockolate | 226.22 ns | 1.316 ns | 1.231 ns | 952 B |
| Moq | 63,747.23 ns | 539.268 ns | 504.432 ns | 6925 B |
| NSubstitute | 3,472.87 ns | 50.268 ns | 47.020 ns | 7088 B |
| FakeItEasy | 3,320.76 ns | 52.641 ns | 49.240 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 76497
  bar [69.18, 319.87, 226.22, 63747.23, 3472.87, 3320.76]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,391.49 ns | 26.307 ns | 27.015 ns | 4608 B |
| Imposter | 1,819.03 ns | 36.389 ns | 61.792 ns | 11192 B |
| Mockolate | 1,818.48 ns | 29.229 ns | 28.707 ns | 5496 B |
| Moq | 348,625.17 ns | 3,556.561 ns | 2,969.889 ns | 34699 B |
| NSubstitute | 10,511.76 ns | 154.353 ns | 136.830 ns | 16762 B |
| FakeItEasy | 11,426.09 ns | 69.338 ns | 61.466 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 418351
  bar [1391.49, 1819.03, 1818.48, 348625.17, 10511.76, 11426.09]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-13T03:23:34.678Z*
