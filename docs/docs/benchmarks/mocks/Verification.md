---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 794.04 ns | 11.762 ns | 11.002 ns | 3080 B |
| Imposter | 728.23 ns | 6.199 ns | 5.495 ns | 4688 B |
| Mockolate | 943.98 ns | 4.571 ns | 3.817 ns | 3144 B |
| Moq | 346,300.40 ns | 4,578.807 ns | 4,058.994 ns | 24325 B |
| NSubstitute | 6,524.12 ns | 70.583 ns | 58.940 ns | 10064 B |
| FakeItEasy | 7,835.22 ns | 33.099 ns | 30.960 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415561
  bar [794.04, 728.23, 943.98, 346300.4, 6524.12, 7835.22]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 84.03 ns | 0.587 ns | 0.520 ns | 384 B |
| Imposter | 340.94 ns | 3.415 ns | 3.195 ns | 2400 B |
| Mockolate | 227.73 ns | 1.137 ns | 1.064 ns | 944 B |
| Moq | 88,742.58 ns | 249.267 ns | 208.149 ns | 7030 B |
| NSubstitute | 3,631.40 ns | 12.364 ns | 11.565 ns | 7088 B |
| FakeItEasy | 3,852.66 ns | 42.529 ns | 37.701 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106492
  bar [84.03, 340.94, 227.73, 88742.58, 3631.4, 3852.66]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,377.25 ns | 9.896 ns | 9.257 ns | 4544 B |
| Imposter | 1,881.33 ns | 35.117 ns | 62.421 ns | 11192 B |
| Mockolate | 2,009.37 ns | 28.286 ns | 26.459 ns | 5488 B |
| Moq | 475,801.63 ns | 6,523.506 ns | 6,102.092 ns | 34699 B |
| NSubstitute | 11,837.61 ns | 46.907 ns | 41.582 ns | 16763 B |
| FakeItEasy | 13,597.49 ns | 210.951 ns | 176.154 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 570962
  bar [1377.25, 1881.33, 2009.37, 475801.63, 11837.61, 13597.49]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-04T03:18:30.135Z*
