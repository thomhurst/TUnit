---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 775.13 ns | 14.946 ns | 15.992 ns | 3080 B |
| Imposter | 707.41 ns | 7.582 ns | 7.092 ns | 4688 B |
| Mockolate | 895.63 ns | 8.798 ns | 7.799 ns | 3104 B |
| Moq | 339,650.33 ns | 3,128.019 ns | 2,925.951 ns | 24325 B |
| NSubstitute | 6,015.25 ns | 31.330 ns | 27.774 ns | 10064 B |
| FakeItEasy | 7,508.41 ns | 37.049 ns | 32.843 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 407581
  bar [775.13, 707.41, 895.63, 339650.33, 6015.25, 7508.41]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 79.85 ns | 1.087 ns | 0.964 ns | 384 B |
| Imposter | 309.96 ns | 4.406 ns | 3.905 ns | 2400 B |
| Mockolate | 234.28 ns | 2.821 ns | 2.639 ns | 904 B |
| Moq | 85,455.70 ns | 865.583 ns | 767.317 ns | 6918 B |
| NSubstitute | 3,596.49 ns | 8.434 ns | 7.889 ns | 7088 B |
| FakeItEasy | 3,540.30 ns | 14.927 ns | 12.465 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 102547
  bar [79.85, 309.96, 234.28, 85455.7, 3596.49, 3540.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,361.26 ns | 24.008 ns | 29.484 ns | 4544 B |
| Imposter | 1,688.19 ns | 27.475 ns | 25.700 ns | 11192 B |
| Mockolate | 1,829.03 ns | 26.332 ns | 24.631 ns | 5400 B |
| Moq | 467,773.53 ns | 2,751.034 ns | 2,438.720 ns | 34699 B |
| NSubstitute | 11,427.27 ns | 166.012 ns | 155.288 ns | 16929 B |
| FakeItEasy | 13,009.05 ns | 159.687 ns | 141.558 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 561329
  bar [1361.26, 1688.19, 1829.03, 467773.53, 11427.27, 13009.05]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-02T03:22:36.142Z*
