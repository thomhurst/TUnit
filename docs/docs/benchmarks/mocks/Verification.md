---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 748.11 ns | 5.563 ns | 5.204 ns | 2968 B |
| Imposter | 695.28 ns | 7.165 ns | 6.703 ns | 4688 B |
| Mockolate | 426.28 ns | 3.964 ns | 3.708 ns | 2240 B |
| Moq | 242,614.76 ns | 1,537.726 ns | 1,438.390 ns | 24324 B |
| NSubstitute | 5,922.58 ns | 50.258 ns | 41.967 ns | 10064 B |
| FakeItEasy | 6,528.41 ns | 80.614 ns | 67.317 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 291138
  bar [748.11, 695.28, 426.28, 242614.76, 5922.58, 6528.41]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.84 ns | 0.314 ns | 0.278 ns | 304 B |
| Imposter | 324.40 ns | 2.505 ns | 2.343 ns | 2400 B |
| Mockolate | 257.70 ns | 1.051 ns | 0.878 ns | 1240 B |
| Moq | 61,803.98 ns | 271.722 ns | 212.143 ns | 6925 B |
| NSubstitute | 3,459.46 ns | 62.312 ns | 61.198 ns | 7088 B |
| FakeItEasy | 3,324.32 ns | 34.288 ns | 30.395 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74165
  bar [51.84, 324.4, 257.7, 61803.98, 3459.46, 3324.32]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,322.76 ns | 21.769 ns | 20.363 ns | 4384 B |
| Imposter | 1,750.40 ns | 30.009 ns | 28.070 ns | 11192 B |
| Mockolate | 1,191.74 ns | 17.650 ns | 16.510 ns | 5376 B |
| Moq | 352,701.37 ns | 2,588.591 ns | 2,294.719 ns | 34699 B |
| NSubstitute | 10,407.19 ns | 50.577 ns | 47.310 ns | 16762 B |
| FakeItEasy | 11,533.65 ns | 89.951 ns | 84.140 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 423242
  bar [1322.76, 1750.4, 1191.74, 352701.37, 10407.19, 11533.65]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-02T03:30:24.417Z*
