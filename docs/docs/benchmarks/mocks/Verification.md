---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 578.27 ns | 2.447 ns | 2.289 ns | 3008 B |
| Imposter | 528.42 ns | 3.413 ns | 3.026 ns | 4688 B |
| Mockolate | 311.99 ns | 0.836 ns | 0.741 ns | 2128 B |
| Moq | 200,027.16 ns | 828.229 ns | 691.609 ns | 24675 B |
| NSubstitute | 4,554.16 ns | 34.811 ns | 30.859 ns | 10064 B |
| FakeItEasy | 5,154.37 ns | 29.490 ns | 26.142 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 240033
  bar [578.27, 528.42, 311.99, 200027.16, 4554.16, 5154.37]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 43.63 ns | 0.299 ns | 0.279 ns | 320 B |
| Imposter | 257.14 ns | 2.028 ns | 1.798 ns | 2400 B |
| Mockolate | 189.04 ns | 2.320 ns | 2.170 ns | 1144 B |
| Moq | 48,752.61 ns | 451.812 ns | 422.626 ns | 6925 B |
| NSubstitute | 2,782.76 ns | 22.225 ns | 20.790 ns | 7088 B |
| FakeItEasy | 2,737.43 ns | 43.577 ns | 40.762 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 58504
  bar [43.63, 257.14, 189.04, 48752.61, 2782.76, 2737.43]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,033.38 ns | 10.839 ns | 10.139 ns | 4472 B |
| Imposter | 1,476.86 ns | 26.229 ns | 24.535 ns | 11192 B |
| Mockolate | 912.25 ns | 12.482 ns | 11.676 ns | 5240 B |
| Moq | 274,837.66 ns | 2,175.599 ns | 2,035.057 ns | 34699 B |
| NSubstitute | 8,452.37 ns | 72.936 ns | 68.225 ns | 16763 B |
| FakeItEasy | 9,379.44 ns | 141.864 ns | 125.759 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 329806
  bar [1033.38, 1476.86, 912.25, 274837.66, 8452.37, 9379.44]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-03T04:04:39.541Z*
