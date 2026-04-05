---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 764.91 ns | 11.322 ns | 10.591 ns | 2984 B |
| Imposter | 769.71 ns | 8.790 ns | 8.222 ns | 4688 B |
| Mockolate | 950.06 ns | 12.760 ns | 11.936 ns | 3152 B |
| Moq | 339,526.09 ns | 1,819.383 ns | 1,701.852 ns | 24325 B |
| NSubstitute | 6,281.01 ns | 63.710 ns | 56.478 ns | 10064 B |
| FakeItEasy | 7,432.16 ns | 52.771 ns | 49.362 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 407432
  bar [764.91, 769.71, 950.06, 339526.09, 6281.01, 7432.16]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 84.48 ns | 0.911 ns | 0.853 ns | 376 B |
| Imposter | 337.65 ns | 6.597 ns | 6.775 ns | 2400 B |
| Mockolate | 225.18 ns | 2.159 ns | 2.019 ns | 952 B |
| Moq | 87,077.29 ns | 491.297 ns | 435.522 ns | 6918 B |
| NSubstitute | 3,594.29 ns | 27.965 ns | 24.790 ns | 7088 B |
| FakeItEasy | 3,489.27 ns | 32.051 ns | 29.981 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 104493
  bar [84.48, 337.65, 225.18, 87077.29, 3594.29, 3489.27]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,380.27 ns | 6.781 ns | 6.343 ns | 4416 B |
| Imposter | 1,817.52 ns | 16.933 ns | 15.839 ns | 11192 B |
| Mockolate | 1,876.89 ns | 9.292 ns | 8.691 ns | 5496 B |
| Moq | 468,799.88 ns | 3,322.719 ns | 3,108.074 ns | 34699 B |
| NSubstitute | 11,571.76 ns | 53.285 ns | 49.843 ns | 16762 B |
| FakeItEasy | 13,488.64 ns | 178.615 ns | 149.151 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 562560
  bar [1380.27, 1817.52, 1876.89, 468799.88, 11571.76, 13488.64]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T03:32:35.400Z*
