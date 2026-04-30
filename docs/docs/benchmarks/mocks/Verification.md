---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 681.89 ns | 1.780 ns | 1.578 ns | 2864 B |
| Imposter | 679.48 ns | 3.115 ns | 2.914 ns | 4688 B |
| Mockolate | 910.96 ns | 2.956 ns | 2.620 ns | 3152 B |
| Moq | 251,640.33 ns | 1,381.093 ns | 1,224.303 ns | 24306 B |
| NSubstitute | 6,113.04 ns | 36.666 ns | 32.504 ns | 10064 B |
| FakeItEasy | 6,638.07 ns | 27.078 ns | 24.004 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 301969
  bar [681.89, 679.48, 910.96, 251640.33, 6113.04, 6638.07]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 47.71 ns | 0.296 ns | 0.277 ns | 304 B |
| Imposter | 328.11 ns | 1.786 ns | 1.671 ns | 2400 B |
| Mockolate | 210.34 ns | 0.643 ns | 0.570 ns | 952 B |
| Moq | 64,147.67 ns | 262.635 ns | 245.669 ns | 6925 B |
| NSubstitute | 3,148.76 ns | 9.839 ns | 9.203 ns | 7088 B |
| FakeItEasy | 3,103.41 ns | 26.252 ns | 24.556 ns | 5218 B |

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
  y-axis "Time (ns)" 0 --> 76978
  bar [47.71, 328.11, 210.34, 64147.67, 3148.76, 3103.41]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,158.00 ns | 4.335 ns | 3.843 ns | 4176 B |
| Imposter | 1,671.45 ns | 21.596 ns | 20.201 ns | 11192 B |
| Mockolate | 1,815.45 ns | 6.855 ns | 6.076 ns | 5496 B |
| Moq | 348,717.86 ns | 2,044.464 ns | 1,812.364 ns | 34670 B |
| NSubstitute | 10,771.87 ns | 88.782 ns | 78.703 ns | 16762 B |
| FakeItEasy | 11,871.86 ns | 36.239 ns | 32.125 ns | 19239 B |

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
  y-axis "Time (ns)" 0 --> 418462
  bar [1158, 1671.45, 1815.45, 348717.86, 10771.87, 11871.86]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-30T03:25:10.403Z*
