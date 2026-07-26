---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 684.49 ns | 3.691 ns | 3.272 ns | 3008 B |
| Imposter | 683.22 ns | 7.615 ns | 7.123 ns | 4688 B |
| Mockolate | 409.52 ns | 6.502 ns | 6.082 ns | 2128 B |
| Moq | 346,587.04 ns | 3,286.102 ns | 2,744.044 ns | 24349 B |
| NSubstitute | 6,414.24 ns | 45.147 ns | 42.230 ns | 10176 B |
| FakeItEasy | 7,244.33 ns | 29.674 ns | 23.167 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415905
  bar [684.49, 683.22, 409.52, 346587.04, 6414.24, 7244.33]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.16 ns | 0.401 ns | 0.335 ns | 320 B |
| Imposter | 316.56 ns | 1.475 ns | 1.307 ns | 2400 B |
| Mockolate | 237.24 ns | 4.619 ns | 4.942 ns | 1144 B |
| Moq | 88,758.62 ns | 502.343 ns | 469.892 ns | 6918 B |
| NSubstitute | 3,684.98 ns | 52.607 ns | 49.209 ns | 7088 B |
| FakeItEasy | 3,553.50 ns | 47.541 ns | 39.699 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106511
  bar [51.16, 316.56, 237.24, 88758.62, 3684.98, 3553.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,285.93 ns | 17.542 ns | 16.409 ns | 4472 B |
| Imposter | 1,771.68 ns | 34.870 ns | 35.809 ns | 11192 B |
| Mockolate | 1,091.72 ns | 11.862 ns | 10.515 ns | 5240 B |
| Moq | 475,853.70 ns | 1,487.814 ns | 1,242.392 ns | 34811 B |
| NSubstitute | 11,446.96 ns | 34.990 ns | 29.218 ns | 16762 B |
| FakeItEasy | 14,762.29 ns | 166.050 ns | 155.323 ns | 19394 B |

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
  y-axis "Time (ns)" 0 --> 571025
  bar [1285.93, 1771.68, 1091.72, 475853.7, 11446.96, 14762.29]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-26T03:33:46.478Z*
