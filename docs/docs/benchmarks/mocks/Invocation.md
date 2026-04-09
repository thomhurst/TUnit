---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 258.5 ns | 106.64 ns | 5.85 ns | 120 B |
| Imposter | 291.1 ns | 77.88 ns | 4.27 ns | 168 B |
| Mockolate | 652.8 ns | 394.46 ns | 21.62 ns | 640 B |
| Moq | 811.7 ns | 114.71 ns | 6.29 ns | 376 B |
| NSubstitute | 714.4 ns | 51.08 ns | 2.80 ns | 304 B |
| FakeItEasy | 1,753.6 ns | 97.68 ns | 5.35 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2105
  bar [258.5, 291.1, 652.8, 811.7, 714.4, 1753.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 181.0 ns | 55.32 ns | 3.03 ns | 88 B |
| Imposter | 292.1 ns | 54.18 ns | 2.97 ns | 168 B |
| Mockolate | 527.3 ns | 189.35 ns | 10.38 ns | 520 B |
| Moq | 523.1 ns | 252.71 ns | 13.85 ns | 296 B |
| NSubstitute | 609.9 ns | 171.99 ns | 9.43 ns | 272 B |
| FakeItEasy | 1,545.9 ns | 399.68 ns | 21.91 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1856
  bar [181, 292.1, 527.3, 523.1, 609.9, 1545.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,052.4 ns | 8,929.36 ns | 489.45 ns | 11936 B |
| Imposter | 28,886.9 ns | 11,038.75 ns | 605.07 ns | 16800 B |
| Mockolate | 67,035.7 ns | 32,626.56 ns | 1,788.37 ns | 64000 B |
| Moq | 79,757.0 ns | 18,676.91 ns | 1,023.74 ns | 37600 B |
| NSubstitute | 72,447.8 ns | 9,213.09 ns | 505.00 ns | 30848 B |
| FakeItEasy | 176,659.2 ns | 71,866.48 ns | 3,939.24 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 211992
  bar [26052.4, 28886.9, 67035.7, 79757, 72447.8, 176659.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-09T03:21:47.332Z*
