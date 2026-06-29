---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 810.43 ns | 3.812 ns | 3.565 ns | 3008 B |
| Imposter | 782.20 ns | 8.363 ns | 7.823 ns | 4688 B |
| Mockolate | 416.61 ns | 1.827 ns | 1.620 ns | 2128 B |
| Moq | 248,739.96 ns | 1,304.413 ns | 1,089.244 ns | 24324 B |
| NSubstitute | 6,206.62 ns | 22.465 ns | 19.915 ns | 10064 B |
| FakeItEasy | 7,054.87 ns | 42.486 ns | 37.663 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 298488
  bar [810.43, 782.2, 416.61, 248739.96, 6206.62, 7054.87]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 58.96 ns | 0.236 ns | 0.221 ns | 320 B |
| Imposter | 370.86 ns | 6.863 ns | 6.420 ns | 2400 B |
| Mockolate | 258.84 ns | 1.322 ns | 1.236 ns | 1144 B |
| Moq | 63,265.56 ns | 223.393 ns | 208.962 ns | 6925 B |
| NSubstitute | 3,777.63 ns | 16.682 ns | 15.604 ns | 7088 B |
| FakeItEasy | 3,588.05 ns | 31.039 ns | 29.034 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75919
  bar [58.96, 370.86, 258.84, 63265.56, 3777.63, 3588.05]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,411.02 ns | 9.082 ns | 8.051 ns | 4472 B |
| Imposter | 2,045.41 ns | 35.000 ns | 29.226 ns | 11192 B |
| Mockolate | 1,276.17 ns | 9.902 ns | 8.778 ns | 5240 B |
| Moq | 358,422.27 ns | 2,512.119 ns | 2,097.733 ns | 34699 B |
| NSubstitute | 11,586.55 ns | 49.836 ns | 44.179 ns | 16762 B |
| FakeItEasy | 12,499.89 ns | 122.664 ns | 114.740 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 430107
  bar [1411.02, 2045.41, 1276.17, 358422.27, 11586.55, 12499.89]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-29T03:30:39.957Z*
